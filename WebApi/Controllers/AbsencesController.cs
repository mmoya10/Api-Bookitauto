using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Absences;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrBranchAdmin)]
    [ServiceFilter(typeof(RequireBranchHeaderFilter))]
    public sealed class AbsencesController : ControllerBase
    {
        private readonly IAbsenceService _svc;
        public AbsencesController(IAbsenceService svc) => _svc = svc;

        // ✏️ Actualiza una ausencia (tipo/fechas/horas/estado); ajusta contadores si procede
        [HttpPut("absences/{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        public async Task<IActionResult> Update(Guid id, [FromBody] AbsenceUpdateRequest req, CancellationToken ct)
        {
            var branchId = BranchContext.GetBranchId(HttpContext);

            await _svc.UpdateAsync(id, branchId, req, ct);
            return Ok(new { id });
        }

        // 🗑️ Elimina una ausencia (si estaba aprobada, revierte contadores)
        [HttpDelete("absences/{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var branchId = BranchContext.GetBranchId(HttpContext);

            await _svc.DeleteAsync(id, branchId, ct);
            return Ok(new { id });
        }
    }
}
