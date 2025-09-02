using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Stock;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/branches/{branchId:guid}/stock")]
    [Authorize]
    [ServiceFilter(typeof(RequireBranchHeaderFilter))]
    public sealed class StockController : ControllerBase
    {
        private readonly IStockService _svc;
        public StockController(IStockService svc) => _svc = svc;

        // GET /stock
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Stock.View")]
        public async Task<IActionResult> Get(
            Guid branchId,
            [FromQuery] string? q,
            [FromQuery] bool? belowMinOnly,
            [FromQuery] int page = 1,
            [FromQuery] int size = 50,
            CancellationToken ct = default)
        {
            page = page <= 0 ? 1 : page;
            size = size <= 0 ? 50 : Math.Min(size, 200);

            var (items, total) = await _svc.GetStockAsync(branchId, q, belowMinOnly, page, size, ct);
            return Ok(new { items, page, size, total });
        }

        // GET /stock/movements
        [HttpGet("movements")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Stock.View")]
        public async Task<IActionResult> Movements(
            Guid branchId,
            [FromQuery] Guid? productId,
            [FromQuery] string? type,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] int page = 1,
            [FromQuery] int size = 50,
            CancellationToken ct = default)
        {
            page = page <= 0 ? 1 : page;
            size = size <= 0 ? 50 : Math.Min(size, 200);

            var (items, total) = await _svc.GetMovementsAsync(branchId, productId, type, from, to, page, size, ct);
            return Ok(new { items, page, size, total });
        }

        // POST /stock/movements
        [HttpPost("movements")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Stock.Manage")]
        public async Task<IActionResult> CreateMovement(Guid branchId, [FromBody] StockMovementCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.CreateMovementAsync(branchId, req, ct);
            return Ok(new { id });
        }

        // DELETE /stock/movements/{id}
        [HttpDelete("/api/stock/movements/{id:guid}")]
        [Authorize(Policy = "Perm.Stock.Manage")]
        public async Task<IActionResult> DeleteMovement(Guid id, CancellationToken ct)
        {
            await _svc.DeleteMovementAsync(id, ct);
            return Ok(new { id });
        }
    }
}
