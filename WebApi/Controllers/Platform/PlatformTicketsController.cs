// Controllers/Platform/PlatformTicketsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Platform.Tickets;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Services.Platform;

namespace WebApi.Controllers.Platform
{
    [ApiController]
    [Route("api/platform/tickets")]
    [Authorize(Policy = AuthorizationPolicies.PlatformOnly)]
    public sealed class PlatformTicketsController : ControllerBase
    {
        private readonly IPlatformTicketService _svc;
        public PlatformTicketsController(IPlatformTicketService svc) => _svc = svc;

        // 📥 Listar tickets
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? severity, CancellationToken ct)
            => Ok(new { items = await _svc.ListAsync(status, severity, ct) });

        // ➕ Crear ticket (p.ej. si vuestro equipo levanta uno manual)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TicketCreateRequest req, CancellationToken ct)
            => Ok(new { id = await _svc.CreateAsync(req, ct) });

        // ✏️ Actualizar ticket (estado/notas)
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] TicketUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateAsync(id, req, ct);
            return Ok(new { id });
        }

        // 🧹 Cerrar ticket
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Close(Guid id, CancellationToken ct)
        {
            await _svc.CloseAsync(id, ct);
            return Ok(new { id });
        }
    }
}
