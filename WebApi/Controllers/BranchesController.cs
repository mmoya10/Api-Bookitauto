using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Branches;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/branches")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrBranchAdmin)]
    [ServiceFilter(typeof(RequireBranchHeaderFilter))]
    public sealed class BranchesController : ControllerBase
    {
        private readonly IBranchService _svc;
        public BranchesController(IBranchService svc) => _svc = svc;

        // ✏️ Actualiza datos básicos de la sucursal (contacto, dirección, timezone, estado, slug)
        [HttpPut("{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        public async Task<IActionResult> Update(Guid id, [FromBody] BranchUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateAsync(id, req, ct);
            return Ok(new { id });
        }

        // 📆 Obtiene los horarios semanales de la sucursal (tramos por día)
        [HttpGet("{id:guid}/schedules")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        public async Task<IActionResult> GetSchedules(Guid id, CancellationToken ct)
            => Ok(new { items = await _svc.GetSchedulesAsync(id, ct) });

        // 🔁 Reemplaza los horarios de la sucursal (valida solapes y rangos)
        [HttpPut("{id:guid}/schedules")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        public async Task<IActionResult> UpdateSchedules(Guid id, [FromBody] BranchScheduleUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateSchedulesAsync(id, req, ct);
            return Ok(new { id });
        }
    }
}
