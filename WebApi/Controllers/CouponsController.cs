using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Coupons;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    [ServiceFilter(typeof(RequireBranchHeaderFilter))]
    public sealed class CouponsController : ControllerBase
    {
        private readonly ICouponService _svc;
        public CouponsController(ICouponService svc) => _svc = svc;

        // 📋 Listar todos los cupones de la sucursal (activos/inactivos, con filtros en futuro)
        [HttpGet("branches/{branchId:guid}/coupons")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        public async Task<IActionResult> List(Guid branchId, CancellationToken ct = default)
            => Ok(new { items = await _svc.ListAsync(branchId, ct) });

        // ➕ Crear un cupón (valida tipo, valor, rango de fechas, código único por branch)
        [HttpPost("branches/{branchId:guid}/coupons")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        public async Task<IActionResult> Create(Guid branchId, [FromBody] CouponCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.CreateAsync(branchId, req, ct);
            return Ok(new { id });
        }

        // ✏️ Actualizar un cupón (edición parcial, revalida coherencia)
        [HttpPut("coupons/{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CouponUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateAsync(id, req, ct);
            return Ok(new { id });
        }

        // 💤 Desactivar un cupón (soft delete para no romper histórico/uso)
        [HttpDelete("coupons/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return Ok(new { id, active = false });
        }

        // 🔄 Activar/Desactivar en bloque todos los cupones de la sucursal
        [HttpPatch("branches/{branchId:guid}/coupons/toggle")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        public async Task<IActionResult> Toggle(Guid branchId, [FromBody] CouponsToggleRequest req, CancellationToken ct)
        {
            await _svc.ToggleAsync(branchId, req.Active, ct);
            return Ok(new { branchId, active = req.Active });
        }
    }
}
