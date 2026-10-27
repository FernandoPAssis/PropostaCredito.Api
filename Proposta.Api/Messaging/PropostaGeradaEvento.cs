namespace Proposta.Api.Messaging
{
    // Mensagem enviada para o serviço de Cartão
    public class PropostaGeradaEvento
    {
        public Guid PropostaId { get; set; }
        public Guid ClienteId { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public int Score { get; set; }
        public bool Aprovada { get; set; }
        public int QuantidadeCartoes { get; set; }
        public decimal LimitePorCartao { get; set; }
    }
}
