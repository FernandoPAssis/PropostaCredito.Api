using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Proposta.Api.Data;
using Proposta.Api.Domain;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Proposta.Api.Messaging
{

    public class ClienteCadastradoConsumer : BackgroundService
    {
        private const string ExchangeOrigem = "cliente-cadastrado";            
        private const string Fila = "proposta.cliente-cadastrado";             // fila deste serviço
        private const string ExchangeDeadLetter = "proposta.dead-letter";
        private const string FilaDeadLetter = "proposta.cliente-cadastrado.dlq";
        private const int MaximoTentativas = 3;

        private readonly IConnection _conexao;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ClienteCadastradoConsumer> _logger;
        private IChannel? _canal;

        public ClienteCadastradoConsumer(IConnection conexao, IServiceScopeFactory scopeFactory, ILogger<ClienteCadastradoConsumer> logger)
        {
            _conexao = conexao;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _canal = await _conexao.CreateChannelAsync(cancellationToken: stoppingToken);

            // 1. Dead-letter: para onde vão as mensagens que falharam em todas as tentativas
            await _canal.ExchangeDeclareAsync(ExchangeDeadLetter, ExchangeType.Fanout, durable: true);
            await _canal.QueueDeclareAsync(FilaDeadLetter, durable: true, exclusive: false, autoDelete: false);
            await _canal.QueueBindAsync(FilaDeadLetter, ExchangeDeadLetter, string.Empty);

            // 2. Fila principal, configurada para mandar as mensagens rejeitadas para o dead-letter
            Dictionary<string, object?> argumentos = new Dictionary<string, object?>();
            argumentos.Add("x-dead-letter-exchange", ExchangeDeadLetter);

            await _canal.ExchangeDeclareAsync(ExchangeOrigem, ExchangeType.Fanout, durable: true);
            await _canal.QueueDeclareAsync(Fila, durable: true, exclusive: false, autoDelete: false, arguments: argumentos);
            await _canal.QueueBindAsync(Fila, ExchangeOrigem, string.Empty);

            // 3. Recebe uma mensagem por vez
            await _canal.BasicQosAsync(0, 1, false);

            // 4. Começa a escutar a fila (autoAck: false = nós confirmamos manualmente)
            AsyncEventingBasicConsumer consumidor = new AsyncEventingBasicConsumer(_canal);
            consumidor.ReceivedAsync += AoReceberMensagemAsync;
            await _canal.BasicConsumeAsync(Fila, autoAck: false, consumer: consumidor);

            _logger.LogInformation("Escutando a fila {Fila}", Fila);
        }

        // Chamado automaticamente a cada mensagem que chega na fila
        private async Task AoReceberMensagemAsync(object sender, BasicDeliverEventArgs mensagem)
        {
            string json = Encoding.UTF8.GetString(mensagem.Body.Span);

            // RETRY: tenta processar até 3 vezes, esperando um pouco entre as tentativas
            for (int tentativa = 1; tentativa <= MaximoTentativas; tentativa++)
            {
                try
                {
                    await ProcessarAsync(json);

                    // ACK: deu certo, o RabbitMQ pode apagar a mensagem
                    await _canal!.BasicAckAsync(mensagem.DeliveryTag, false);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao processar mensagem (tentativa {Tentativa} de {Maximo})", tentativa, MaximoTentativas);

                    if (tentativa < MaximoTentativas)
                        await Task.Delay(TimeSpan.FromSeconds(2 * tentativa));
                }
            }

            // DEAD-LETTER: falhou em todas as tentativas.
            // NACK sem devolver para a fila -> o RabbitMQ manda para a fila .dlq
            _logger.LogError("Mensagem enviada para a dead-letter queue: {Json}", json);
            await _canal!.BasicNackAsync(mensagem.DeliveryTag, false, false);
        }

        private async Task ProcessarAsync(string json)
        {
            ClienteCadastradoEvento? evento = JsonSerializer.Deserialize<ClienteCadastradoEvento>(json);
            if (evento == null)
                throw new InvalidOperationException("Mensagem inválida.");

            // o consumidor vive a aplicação inteira; o DbContext deve ser criado por mensagem
            using IServiceScope scope = _scopeFactory.CreateScope();
            PropostaDbContext db = scope.ServiceProvider.GetRequiredService<PropostaDbContext>();
            RabbitMqPublisher publisher = scope.ServiceProvider.GetRequiredService<RabbitMqPublisher>();

            // IDEMPOTÊNCIA: se a mesma mensagem chegar duas vezes, não cria outra proposta
            PropostaCredito? proposta = await db.Propostas.FirstOrDefaultAsync(p => p.ClienteId == evento.ClienteId);

            if (proposta == null)
            {
                proposta = RegraCredito.GerarProposta(evento.ClienteId, evento.Nome, evento.RendaMensal);
                db.Propostas.Add(proposta);
                await db.SaveChangesAsync();

                _logger.LogInformation("Proposta gerada para o cliente {ClienteId}: score {Score}, aprovada: {Aprovada}",
                    proposta.ClienteId, proposta.Score, proposta.Aprovada);
            }

            // Publica o resultado. Se a proposta já existia (ex.: retry depois de falha na publicação),
            // publica de novo; o serviço de Cartão ignora duplicadas.
            PropostaGeradaEvento propostaGerada = new PropostaGeradaEvento
            {
                PropostaId = proposta.Id,
                ClienteId = proposta.ClienteId,
                NomeCliente = proposta.NomeCliente,
                Score = proposta.Score,
                Aprovada = proposta.Aprovada,
                QuantidadeCartoes = proposta.QuantidadeCartoes,
                LimitePorCartao = proposta.LimitePorCartao
            };

            await publisher.PublicarPropostaGeradaAsync(propostaGerada);
        }

        public override void Dispose()
        {
            _canal?.Dispose();
            base.Dispose();
        }
    }
}