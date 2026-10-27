using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Proposta.Api.Messaging
{
    public class RabbitMqPublisher
    {
        public const string ExchangePropostaGerada = "proposta-gerada";

        private readonly IConnection _conexao;

        public RabbitMqPublisher(IConnection conexao)
        {
            _conexao = conexao;
        }

        public async Task PublicarPropostaGeradaAsync(PropostaGeradaEvento evento)
        {
            await using IChannel canal = await _conexao.CreateChannelAsync();

            await canal.ExchangeDeclareAsync(ExchangePropostaGerada, ExchangeType.Fanout, durable: true);

            string json = JsonSerializer.Serialize(evento);
            byte[] corpo = Encoding.UTF8.GetBytes(json);

            BasicProperties propriedades = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            await canal.BasicPublishAsync(ExchangePropostaGerada, string.Empty, false, propriedades, corpo);
        }
    }
}