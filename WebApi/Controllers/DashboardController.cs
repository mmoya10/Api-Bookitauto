using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Dashboard;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrBranchAdmin)]
    [ServiceFilter(typeof(RequireBranchHeaderFilter))]
    public sealed class DashboardController : ControllerBase
    {
        private readonly IDashboardService _svc;
        public DashboardController(IDashboardService svc) => _svc = svc;

        // 📊 Resumen de KPIs del dashboard (bookings, revenue, top servicios, etc.) en un rango
        // Lee el BranchId del header (X-Branch-Id); rango opcional con ?from=&to= (UTC)
        [HttpGet("summary")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        public async Task<IActionResult> Summary([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
        {
            var branchId = BranchContext.GetBranchId(HttpContext);
            var data = await _svc.GetSummaryAsync(branchId, from, to, ct);
            return Ok(new { kpis = data });
        }
    }
}
