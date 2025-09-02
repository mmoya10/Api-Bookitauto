using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Cash;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/branches/{branchId:guid}/cash")]
    [Authorize]
    [ServiceFilter(typeof(RequireBranchHeaderFilter))]
    public sealed class CashController : ControllerBase
    {
        private readonly ICashService _svc;
        public CashController(ICashService svc) => _svc = svc;

        // GET sesiones (abierta + últimas 20)
        [HttpGet("sessions")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Cash.View")]
        public async Task<IActionResult> Sessions(Guid branchId, [FromQuery] int take = 20, CancellationToken ct = default)
        {
            take = take <= 0 ? 20 : Math.Min(take, 100);
            var (open, last) = await _svc.GetSessionsAsync(branchId, take, ct);
            return Ok(new { open, last });
        }

        // POST abrir sesión
        [HttpPost("sessions")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Cash.Manage")]
        public async Task<IActionResult> Open(Guid branchId, [FromBody] CashSessionOpenRequest req, CancellationToken ct)
        {
            var id = await _svc.OpenAsync(branchId, req, ct);
            return Ok(new { id });
        }

        // PUT cerrar sesión
        [HttpPut("/api/cash/sessions/{id:guid}/close")]
        [Authorize(Policy = "Perm.Cash.Manage")]
        public async Task<IActionResult> Close(Guid id, [FromBody] CashSessionCloseRequest req, CancellationToken ct)
        {
            await _svc.CloseAsync(id, req, ct);
            return Ok(new { id });
        }

        // GET movimientos de una sesión
        [HttpGet("/api/cash/sessions/{id:guid}/movements")]
        [Authorize(Policy = "Perm.Cash.View")]
        public async Task<IActionResult> Movements(Guid id, [FromQuery] int page = 1, [FromQuery] int size = 100, CancellationToken ct = default)
        {
            page = page <= 0 ? 1 : page;
            size = size <= 0 ? 100 : Math.Min(size, 200);

            var (items, total) = await _svc.GetMovementsAsync(id, page, size, ct);
            return Ok(new { items, page, size, total });
        }

        // POST crear movimiento
        [HttpPost("/api/cash/sessions/{id:guid}/movements")]
        [Authorize(Policy = "Perm.Cash.Manage")]
        public async Task<IActionResult> CreateMovement(Guid id, [FromBody] CashMovementCreateRequest req, CancellationToken ct)
        {
            var movementId = await _svc.CreateMovementAsync(id, req, ct);
            return Ok(new { movementId });
        }

        // DELETE movimiento
        [HttpDelete("/api/cash/movements/{id:guid}")]
        [Authorize(Policy = "Perm.Cash.Manage")]
        public async Task<IActionResult> DeleteMovement(Guid id, CancellationToken ct)
        {
            await _svc.DeleteMovementAsync(id, ct);
            return Ok(new { id });
        }
    }
}
