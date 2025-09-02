using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Waitlist;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize] // cliente autenticado
    public sealed class WaitlistController : ControllerBase
    {
        private readonly IWaitlistService _svc;
        public WaitlistController(IWaitlistService svc) => _svc = svc;

        // ➕ Crear nueva entrada en la lista de espera
        [HttpPost("waitlist")]
        public async Task<IActionResult> Create([FromBody] WaitlistCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.CreateAsync(req, ct);
            return Ok(new { id });
        }

        // 👤 Listar mis entradas activas en la lista de espera
        [HttpGet("waitlist/mine")]
        public async Task<IActionResult> Mine(CancellationToken ct)
            => Ok(new { items = await _svc.MineAsync(ct) });

        // ❌ Cancelar/eliminar una entrada de la lista de espera
        [HttpDelete("waitlist/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return Ok(new { id });
        }
    }
}
