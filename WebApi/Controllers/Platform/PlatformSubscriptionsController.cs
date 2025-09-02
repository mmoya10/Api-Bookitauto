// Controllers/Platform/PlatformSubscriptionsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Platform.Subscriptions;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Services.Platform;

namespace WebApi.Controllers.Platform
{
    [ApiController]
    [Route("api/platform/subscriptions")]
    [Authorize(Policy = AuthorizationPolicies.PlatformOnly)]
    public sealed class PlatformSubscriptionsController : ControllerBase
    {
        private readonly IPlatformSubscriptionService _svc;
        public PlatformSubscriptionsController(IPlatformSubscriptionService svc) => _svc = svc;

        // 📦 Listar planes
        [HttpGet("plans")]
        public async Task<IActionResult> Plans(CancellationToken ct)
            => Ok(new { items = await _svc.ListPlansAsync(ct) });

        // ➕ Crear plan
        [HttpPost("plans")]
        public async Task<IActionResult> CreatePlan([FromBody] PlanCreateRequest req, CancellationToken ct)
            => Ok(new { id = await _svc.CreatePlanAsync(req, ct) });

        // ✏️ Actualizar plan
        [HttpPut("plans/{id:guid}")]
        public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] PlanUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdatePlanAsync(id, req, ct);
            return Ok(new { id });
        }

        // 🗑️ Eliminar plan
        [HttpDelete("plans/{id:guid}")]
        public async Task<IActionResult> DeletePlan(Guid id, CancellationToken ct)
        {
            await _svc.DeletePlanAsync(id, ct);
            return Ok(new { id });
        }

        // 🔗 Ver suscripción de un negocio
        [HttpGet("business/{businessId:guid}")]
        public async Task<IActionResult> GetBusinessSub(Guid businessId, CancellationToken ct)
            => Ok(await _svc.GetBusinessSubscriptionAsync(businessId, ct));

        // 🔗 Asignar suscripción a un negocio
        [HttpPost("business/{businessId:guid}")]
        public async Task<IActionResult> Assign(Guid businessId, [FromBody] AssignSubscriptionRequest req, CancellationToken ct)
            => Ok(new { subscriptionId = await _svc.AssignSubscriptionAsync(businessId, req, ct) });

        // ✏️ Actualizar suscripción de un negocio
        [HttpPut("business/{businessId:guid}")]
        public async Task<IActionResult> UpdateAssignment(Guid businessId, [FromBody] AssignSubscriptionRequest req, CancellationToken ct)
        {
            await _svc.UpdateBusinessSubscriptionAsync(businessId, req, ct);
            return Ok(new { businessId });
        }

        // 📄 Facturas/usages de un negocio (últimos)
        [HttpGet("business/{businessId:guid}/invoices")]
        public async Task<IActionResult> Invoices(Guid businessId, CancellationToken ct)
            => Ok(new { items = await _svc.ListBusinessInvoicesAsync(businessId, ct) });
    }
}
