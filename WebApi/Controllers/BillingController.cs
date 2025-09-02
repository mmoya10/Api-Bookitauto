using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Billing;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/billing")]
    [Authorize(Roles = "Admin")] // solo Admin
    public sealed class BillingController : ControllerBase
    {
        private readonly IBillingService _svc;
        public BillingController(IBillingService svc) => _svc = svc;

        // 🔎 Devuelve el estado de la suscripción (plan, estado, periodo actual, etc.)
        [HttpGet("subscription")]
        public async Task<IActionResult> Subscription(CancellationToken ct)
            => Ok(await _svc.GetSubscriptionAsync(ct));

        // 📄 Lista de facturas (últimas N)
        [HttpGet("invoices")]
        public async Task<IActionResult> Invoices(CancellationToken ct)
            => Ok(new { items = await _svc.ListInvoicesAsync(ct) });

        // ⬇️ Descarga el PDF de una factura
        [HttpGet("invoices/{id:guid}/download")]
        public async Task<IActionResult> Download(Guid id, CancellationToken ct)
        {
            var (content, fileName, contentType) = await _svc.DownloadInvoicePdfAsync(id, ct);
            return File(content, contentType, fileName);
        }

        // ❗ Solicita la cancelación (no inmediata): registra petición y envía email
        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel([FromBody] CancelSubscriptionRequest req, CancellationToken ct)
        {
            await _svc.RequestCancelAsync(req, ct);
            return Ok(new { requested = true });
        }
    }
}
