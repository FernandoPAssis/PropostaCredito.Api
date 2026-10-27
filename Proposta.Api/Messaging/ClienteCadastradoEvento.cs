namespace Proposta.Api.Messaging
{
    public class ClienteCadastradoEvento
    {
        public Guid ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal RendaMensal { get; set; }
        public DateTime DataCadastro { get; set; }
    }
}
