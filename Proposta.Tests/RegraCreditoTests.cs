using System;
using System.Collections.Generic;
using System.Text;
using Proposta.Api.Domain;

namespace Proposta.Tests
{
    public class RegraCreditoTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1000, 100)]
        [InlineData(5000, 500)]
        [InlineData(7500, 750)]
        [InlineData(50000, 1000)]   // limitado ao score máximo
        public void CalcularScore_ComRendaInformada_RetornaScoreEsperado(int renda, int scoreEsperado)
        {
            int score = RegraCredito.CalcularScore(renda);

            Assert.Equal(scoreEsperado, score);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 0)]
        [InlineData(101, 1)]
        [InlineData(500, 1)]
        [InlineData(501, 2)]
        [InlineData(1000, 2)]
        public void QuantidadeCartoes_PorFaixaDeScore_RetornaQuantidadeEsperada(int score, int quantidadeEsperada)
        {
            int quantidade = RegraCredito.QuantidadeCartoes(score);

            Assert.Equal(quantidadeEsperada, quantidade);
        }

        [Theory]
        [InlineData(100, 0)]
        [InlineData(101, 1000)]
        [InlineData(500, 1000)]
        [InlineData(501, 5000)]
        [InlineData(1000, 5000)]
        public void LimitePorCartao_PorFaixaDeScore_RetornaLimiteEsperado(int score, int limiteEsperado)
        {
            decimal limite = RegraCredito.LimitePorCartao(score);

            Assert.Equal((decimal)limiteEsperado, limite);
        }

        [Fact]
        public void GerarProposta_QuandoScoreAte100_PropostaNegadaSemCartoes()
        {
            // Arrange
            Guid clienteId = Guid.NewGuid();

            // Act
            PropostaCredito proposta = RegraCredito.GerarProposta(clienteId, "Cliente Teste", 800);

            // Assert
            Assert.Equal(80, proposta.Score);
            Assert.False(proposta.Aprovada);
            Assert.Equal(0, proposta.QuantidadeCartoes);
        }

        [Fact]
        public void GerarProposta_QuandoScoreAcimaDe500_AprovadaComDoisCartoes()
        {
            Guid clienteId = Guid.NewGuid();

            PropostaCredito proposta = RegraCredito.GerarProposta(clienteId, "Cliente Teste", 8000);

            Assert.Equal(clienteId, proposta.ClienteId);
            Assert.Equal(800, proposta.Score);
            Assert.True(proposta.Aprovada);
            Assert.Equal(2, proposta.QuantidadeCartoes);
            Assert.Equal(5000m, proposta.LimitePorCartao);
        }
    }
}
