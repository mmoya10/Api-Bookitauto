using Microsoft.AspNetCore.Mvc;
using WebApi.Infrastructure.Services.Payments;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public sealed class WebhooksController : ControllerBase
    {
        private readonly IPaymentWebhookService _svc;
        public WebhooksController(IPaymentWebhookService svc) => _svc = svc;

        // 💳 Entrada de webhooks de pagos (Stripe por ahora)
        [HttpPost("payments/stripe")]
        public async Task<IActionResult> Stripe(CancellationToken ct)
        {
            using var reader = new StreamReader(Request.Body);
            var raw = await reader.ReadToEndAsync();

            await _svc.HandleAsync("stripe", raw, Request.Headers, ct);
            return Ok(new { received = true });
        }

        // Si en el futuro añades otros proveedores:
        // [HttpPost("payments/{provider}")]
        // public async Task<IActionResult> Any(string provider, CancellationToken ct) { ... }
    }
}
