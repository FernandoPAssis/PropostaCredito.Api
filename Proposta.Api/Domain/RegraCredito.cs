namespace Proposta.Api.Domain
{
    // Regras de concessão de crédito (tabela de score do desafio)
    //   0 a 100    -> negado,   0 cartões
    //   101 a 500  -> aprovado, 1 cartão de R$ 1.000,00
    //   501 a 1000 -> aprovado, 2 cartões de R$ 5.000,00 cada
    public class RegraCredito
    {
        public const int ScoreMaximo = 1000;

        // Regra de score adotada: renda mensal dividida por 10, limitada a 1000.
        // Ex.: renda de R$ 5.000 -> score 500; renda de R$ 10.000 ou mais -> score 1000.
        public static int CalcularScore(decimal rendaMensal)
        {
            if (rendaMensal <= 0)
                return 0;

            int score = (int)(rendaMensal / 10);

            if (score > ScoreMaximo)
                return ScoreMaximo;

            return score;
        }

        public static int QuantidadeCartoes(int score)
        {
            if (score <= 100)
                return 0;

            if (score <= 500)
                return 1;

            return 2;
        }

        public static decimal LimitePorCartao(int score)
        {
            if (score <= 100)
                return 0;

            if (score <= 500)
                return 1000;

            return 5000;
        }

        public static PropostaCredito GerarProposta(Guid clienteId, string nomeCliente, decimal rendaMensal)
        {
            int score = CalcularScore(rendaMensal);
            int quantidade = QuantidadeCartoes(score);

            PropostaCredito proposta = new PropostaCredito
            {
                Id = Guid.NewGuid(),
                ClienteId = clienteId,
                NomeCliente = nomeCliente,
                Score = score,
                Aprovada = quantidade > 0,
                QuantidadeCartoes = quantidade,
                LimitePorCartao = LimitePorCartao(score),
                DataCriacao = DateTime.UtcNow
            };

            return proposta;
        }
    }
}
