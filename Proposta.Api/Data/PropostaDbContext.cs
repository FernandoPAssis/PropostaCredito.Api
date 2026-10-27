using Microsoft.EntityFrameworkCore;
using Proposta.Api.Domain;

namespace Proposta.Api.Data
{
    public class PropostaDbContext : DbContext
    {
        public PropostaDbContext(DbContextOptions<PropostaDbContext> options) : base(options)
        {
        }

        public DbSet<PropostaCredito> Propostas
        {
            get { return Set<PropostaCredito>(); }
        }
    }
}
