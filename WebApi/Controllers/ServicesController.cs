using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Services;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/branches/{branchId:guid}/services")]
    [Authorize]
    [ServiceFilter(typeof(RequireBranchHeaderFilter))]
    public sealed class ServicesController : ControllerBase
    {
        private readonly IServiceCatalogService _svc;
        public ServicesController(IServiceCatalogService svc) => _svc = svc;

        // SERVICES
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Services.View")]
        public async Task<IActionResult> List(Guid branchId, CancellationToken ct = default)
            => Ok(new { items = await _svc.ListAsync(branchId, ct) });

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Services.Manage")]
        public async Task<IActionResult> Create(Guid branchId, [FromBody] ServiceCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.CreateAsync(branchId, req, ct);
            return Ok(new { id });
        }

        [HttpPut("/api/services/{id:guid}")]
        [Authorize(Policy = "Perm.Services.Manage")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ServiceUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateAsync(id, req, ct);
            return Ok(new { id });
        }

        [HttpDelete("/api/services/{id:guid}")]
        [Authorize(Policy = "Perm.Services.Manage")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return Ok(new { id, active = false });
        }

        // OPTIONS
        [HttpPost("/api/services/{serviceId:guid}/options")]
        [Authorize(Policy = "Perm.Services.Manage")]
        public async Task<IActionResult> AddOption(Guid serviceId, [FromBody] ServiceOptionCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.AddOptionAsync(serviceId, req, ct);
            return Ok(new { id });
        }

        [HttpPut("/api/service-options/{id:guid}")]
        [Authorize(Policy = "Perm.Services.Manage")]
        public async Task<IActionResult> UpdateOption(Guid id, [FromBody] ServiceOptionUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateOptionAsync(id, req, ct);
            return Ok(new { id });
        }

        [HttpDelete("/api/service-options/{id:guid}")]
        [Authorize(Policy = "Perm.Services.Manage")]
        public async Task<IActionResult> DeleteOption(Guid id, CancellationToken ct)
        {
            await _svc.DeleteOptionAsync(id, ct);
            return Ok(new { id });
        }

        // EXTRAS catálogo por branch
        [HttpGet("/api/branches/{branchId:guid}/extras")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Services.View")]
        public async Task<IActionResult> ExtrasList(Guid branchId, CancellationToken ct = default)
            => Ok(new { items = await _svc.ExtrasListAsync(branchId, ct) });

        [HttpPost("/api/branches/{branchId:guid}/extras")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Services.Manage")]
        public async Task<IActionResult> ExtrasCreate(Guid branchId, [FromBody] ExtraCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.ExtrasCreateAsync(branchId, req, ct);
            return Ok(new { id });
        }

        [HttpPut("/api/extras/{id:guid}")]
        [Authorize(Policy = "Perm.Services.Manage")]
        public async Task<IActionResult> ExtrasUpdate(Guid id, [FromBody] ExtraUpdateRequest req, CancellationToken ct)
        {
            await _svc.ExtrasUpdateAsync(id, req, ct);
            return Ok(new { id });
        }

        [HttpDelete("/api/extras/{id:guid}")]
        [Authorize(Policy = "Perm.Services.Manage")]
        public async Task<IActionResult> ExtrasDelete(Guid id, CancellationToken ct)
        {
            await _svc.ExtrasDeleteAsync(id, ct);
            return Ok(new { id });
        }

        // Vincular extras a servicio
        [HttpPost("/api/services/{serviceId:guid}/extras/{extraId:guid}")]
        [Authorize(Policy = "Perm.Services.Manage")]
        public async Task<IActionResult> LinkExtra(Guid serviceId, Guid extraId, CancellationToken ct)
        {
            await _svc.LinkExtraAsync(serviceId, extraId, ct);
            return Ok(new { serviceId, extraId });
        }

        [HttpDelete("/api/services/{serviceId:guid}/extras/{extraId:guid}")]
        [Authorize(Policy = "Perm.Services.Manage")]
        public async Task<IActionResult> UnlinkExtra(Guid serviceId, Guid extraId, CancellationToken ct)
        {
            await _svc.UnlinkExtraAsync(serviceId, extraId, ct);
            return Ok(new { serviceId, extraId });
        }
    }
}
