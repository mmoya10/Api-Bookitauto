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

    if (data == null)
    {
        // Datos de prueba (TST)
        data = new DashboardSummaryResponse
        {
            From = from ?? DateTimeOffset.UtcNow.AddDays(-7),
            To = to ?? DateTimeOffset.UtcNow,
            BookingsTotal = 12,
            BookingsCompleted = 10,
            BookingsCancelled = 1,
            BookingsNoShow = 0,
            BookingsUpcoming = 1,
            RevenueTotal = 450.75m,
            RevenueServices = 300.50m,
            RevenueProducts = 150.25m,
            NewUsers = 3,
            WaitlistActive = 2,
            TopServices = new List<DashboardSummaryResponse.TopServiceItem>
            {
                new() { ServiceId = Guid.NewGuid(), Name = "Corte de pelo", Count = 5, Revenue = 200 },
                new() { ServiceId = Guid.NewGuid(), Name = "Manicura", Count = 3, Revenue = 100 },
            }
        };
    }

    return Ok(new { kpis = data });
}

    }
}
