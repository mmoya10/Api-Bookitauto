// Controllers/Platform/PlatformDashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Infrastructure.Policies;

namespace WebApi.Controllers.Platform
{
    [ApiController]
    [Route("api/platform/dashboard")]
    [Authorize(Policy = AuthorizationPolicies.PlatformOnly)]
    public sealed class PlatformDashboardController : ControllerBase
    {
        // 📊 Resumen global de cuentas/tickets/estado servicios
        [HttpGet("summary")]
        public IActionResult Summary()
            => Ok(new { kpis = new { accounts = 0, tickets_open = 0 }, status = new { web = "ok", api = "ok", app = "ok" } });
    }
}
