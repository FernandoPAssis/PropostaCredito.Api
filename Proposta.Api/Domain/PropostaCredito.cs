namespace Proposta.Api.Domain
{
    public class PropostaCredito
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public int Score { get; set; }
        public bool Aprovada { get; set; }
        public int QuantidadeCartoes { get; set; }
        public decimal LimitePorCartao { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
