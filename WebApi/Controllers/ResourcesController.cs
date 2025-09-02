using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Resources;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/branches/{branchId:guid}/resources")]
    [Authorize]
    [ServiceFilter(typeof(RequireBranchHeaderFilter))]
    public sealed class ResourcesController : ControllerBase
    {
        private readonly IResourceService _svc;
        public ResourcesController(IResourceService svc) => _svc = svc;

        // 📋 Lista los recursos/espacios de la sucursal (con cantidades)
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Resources.View")]
        public async Task<IActionResult> List(Guid branchId, CancellationToken ct = default)
            => Ok(new { items = await _svc.ListAsync(branchId, ct) });

        // ➕ Crea un recurso (nombre único por sucursal, cantidad > 0)
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Resources.Manage")]
        public async Task<IActionResult> Create(Guid branchId, [FromBody] ResourceCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.CreateAsync(branchId, req, ct);
            return Ok(new { id });
        }

        // ✏️ Actualiza un recurso (renombrar/editar cantidad/descripcion)
        [HttpPut("/api/resources/{id:guid}")]
        [Authorize(Policy = "Perm.Resources.Manage")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ResourceUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateAsync(id, req, ct);
            return Ok(new { id });
        }

        // 🗑️ Elimina un recurso (si no está vinculado a servicios)
        [HttpDelete("/api/resources/{id:guid}")]
        [Authorize(Policy = "Perm.Resources.Manage")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return Ok(new { id });
        }
    }
}
