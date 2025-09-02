using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Products;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/branches/{branchId:guid}/products")]
    [Authorize]
    [ServiceFilter(typeof(RequireBranchHeaderFilter))]
    public sealed class ProductsController : ControllerBase
    {
        private readonly IProductService _svc;

        public ProductsController(IProductService svc) => _svc = svc;

        // GET list with filtros básicos
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Products.View")]
        public async Task<IActionResult> List(
            Guid branchId,
            [FromQuery] string? q,
            [FromQuery] Guid? categoryId,
            [FromQuery] bool? active,
            [FromQuery] string? sort,     // name|sku|price|createdAt (createdAt por defecto)
            [FromQuery] bool desc = true,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20,
            CancellationToken ct = default)
        {
            page = page <= 0 ? 1 : page;
            size = size <= 0 ? 20 : Math.Min(size, 200);

            var (items, total) = await _svc.ListAsync(branchId, q, categoryId, active, sort, desc, page, size, ct);
            return Ok(new { items, page, size, total });
        }

        // POST create
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Products.Manage")]
        public async Task<IActionResult> Create(Guid branchId, [FromBody] ProductCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.CreateAsync(branchId, req, ct);
            return Ok(new { id });
        }

        // PUT update
        [HttpPut("/api/products/{id:guid}")]
        [Authorize(Policy = "Perm.Products.Manage")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ProductUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateAsync(id, req, ct);
            return Ok(new { id });
        }

        // DELETE (soft delete)
        [HttpDelete("/api/products/{id:guid}")]
        [Authorize(Policy = "Perm.Products.Manage")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return Ok(new { id });
        }
    }
}
