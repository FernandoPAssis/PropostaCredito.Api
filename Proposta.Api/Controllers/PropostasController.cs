using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proposta.Api.Data;
using Proposta.Api.Domain;

namespace Proposta.Api.Controllers
{
    [ApiController]
    [Route("api/v1/propostas")]
    public class PropostasController : ControllerBase
    {
        private readonly PropostaDbContext _db;

        public PropostasController(PropostaDbContext db)
        {
            _db = db;
        }

        [HttpGet("{clienteId:guid}")]
        public async Task<IActionResult> ObterPorCliente(Guid clienteId)
        {
            // equivale a: SELECT TOP 1 * FROM Propostas WHERE ClienteId = @clienteId
            PropostaCredito? proposta = await _db.Propostas.FirstOrDefaultAsync(p => p.ClienteId == clienteId);

            if (proposta == null)
                return NotFound();                  // 404

            return Ok(proposta);                    // 200
        }
    }
}