// Controllers/Platform/PlatformAccountsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Platform.Accounts;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Services;
using WebApi.Infrastructure.Services.Platform;

namespace WebApi.Controllers.Platform
{
    [ApiController]
    [Route("api/platform/accounts")]
    [Authorize(Policy = AuthorizationPolicies.PlatformOnly)]
    public sealed class PlatformAccountsController : ControllerBase
    {
        private readonly IPlatformAccountService _svc;
        private readonly IImpersonationService _imp;

        public PlatformAccountsController(IPlatformAccountService svc, IImpersonationService imp)
        {
            _svc = svc; _imp = imp;
        }

        // 📚 Lista de cuentas (negocios)
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? q, CancellationToken ct)
            => Ok(new { items = await _svc.ListAsync(q, ct) });

        // 👥 Staff de una cuenta
        [HttpGet("{businessId:guid}/staff")]
        public async Task<IActionResult> Staff(Guid businessId, CancellationToken ct)
            => Ok(new { items = await _svc.StaffByBusinessAsync(businessId, ct) });

        // 🔑 Impersonar como un staff
        [HttpPost("impersonate/{staffId:guid}")]
        public async Task<IActionResult> Impersonate(Guid staffId, CancellationToken ct)
        {
            var (token, staff) = await _imp.ImpersonateAsync(staffId,
                reason: "support",
                ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                ua: Request.Headers.UserAgent.ToString(),
                ct);

            return Ok(new ImpersonateResponse
            {
                Token = token,
                StaffId = staff.Id,
                BusinessId = staff.Branch.BusinessId,
                BranchId = staff.BranchId
            });
        }

        // ➕ Crear cuenta
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AccountCreateRequest req, CancellationToken ct)
            => Ok(new { id = await _svc.CreateAsync(req, ct) });

        // ✏️ Editar cuenta
        [HttpPut("{businessId:guid}")]
        public async Task<IActionResult> Update(Guid businessId, [FromBody] AccountUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateAsync(businessId, req, ct);
            return Ok(new { businessId });
        }

        // 🗑️ Cancelar o desactivar cuenta
        [HttpDelete("{businessId:guid}")]
        public async Task<IActionResult> CancelOrDelete(Guid businessId, CancellationToken ct)
        {
            await _svc.CancelOrDeleteAsync(businessId, ct);
            return Ok(new { businessId });
        }
    }
}
