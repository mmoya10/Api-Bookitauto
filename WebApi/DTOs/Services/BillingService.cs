using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Billing;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface IBillingService
    {
        Task<SubscriptionDto> GetSubscriptionAsync(CancellationToken ct);
        Task<IReadOnlyList<InvoiceListItemDto>> ListInvoicesAsync(CancellationToken ct);
        Task<(byte[] content, string fileName, string contentType)> DownloadInvoicePdfAsync(Guid invoiceId, CancellationToken ct);
        Task RequestCancelAsync(CancelSubscriptionRequest req, CancellationToken ct);
    }

    public sealed class BillingService : IBillingService
    {
        private readonly AppDbContext _db;

        // Si ya tienes un EmailService, puedes inyectarlo aquí.
        public BillingService(AppDbContext db) { _db = db; }

        public async Task<SubscriptionDto> GetSubscriptionAsync(CancellationToken ct)
        {
            // TODO: Traer de tu tabla de suscripciones o PSP. Mock mínimo:
            return await Task.FromResult(new SubscriptionDto
            {
                Plan = "starter",
                Status = "active",
                CurrentPeriodEnd = DateTimeOffset.UtcNow.AddMonths(1),
                CancelAtPeriodEnd = false,
                Provider = "stripe",
                ExternalId = "sub_123"
            });
        }

        public async Task<IReadOnlyList<InvoiceListItemDto>> ListInvoicesAsync(CancellationToken ct)
        {
            // TODO: Traer de tu tabla de invoices o PSP. Mock mínimo:
            var items = new List<InvoiceListItemDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IssuedAt = DateTimeOffset.UtcNow.AddDays(-28),
                    Total = 29.00m,
                    Currency = "EUR",
                    Status = "paid",
                    ExternalId = "in_001"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    IssuedAt = DateTimeOffset.UtcNow.AddDays(-58),
                    Total = 29.00m,
                    Currency = "EUR",
                    Status = "paid",
                    ExternalId = "in_000"
                }
            };
            return await Task.FromResult(items);
        }

        public async Task<(byte[] content, string fileName, string contentType)> DownloadInvoicePdfAsync(Guid invoiceId, CancellationToken ct)
        {
            // TODO: traer PDF real del PSP. Mock: PDF vacío
            var fileName = $"invoice-{invoiceId}.pdf";
            var pdfBytes = Array.Empty<byte>();
            return await Task.FromResult((pdfBytes, fileName, "application/pdf"));
        }

        public async Task RequestCancelAsync(CancelSubscriptionRequest req, CancellationToken ct)
        {
            // Crea un registro para enviar email de solicitud de cancelación (no cancela aún)
            _db.NotificationLogs.Add(new Domain.Entities.NotificationLog
            {
                Id = Guid.NewGuid(),
                BranchId = Guid.Empty, // opcional: si tu billing es por negocio, puedes guardar una branch "principal" o BusinessId en payload
                BookingId = null,
                Name = "billing.cancel_request",
                Channel = "email",
                Status = "pending",
                Retries = 0,
                PayloadJson = System.Text.Json.JsonDocument.Parse($@"
                {{
                    ""reason"": {System.Text.Json.JsonSerializer.Serialize(req.Reason)},
                    ""contactEmail"": {System.Text.Json.JsonSerializer.Serialize(req.ContactEmail)},
                    ""requestedAt"": ""{DateTimeOffset.UtcNow:O}""
                }}"),
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            // Si tuvieras IEmailSender, podrías enviarlo aquí sincolero y registrar log.
        }
    }
}
