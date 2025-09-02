using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Billing;
using WebApi.Infrastructure.Auth;
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
        private readonly ICurrentUser _me;

        public BillingService(AppDbContext db, ICurrentUser me)
        {
            _db = db; _me = me;
        }

        public async Task<SubscriptionDto> GetSubscriptionAsync(CancellationToken ct)
        {
            // Negocio actual a partir del staff Admin autenticado
            var staffId = _me.StaffId ?? throw new InvalidOperationException("Usuario no autenticado.");
            var staff = await _db.Staff.Include(s => s.Branch)
                                       .FirstAsync(s => s.Id == staffId, ct);
            var businessId = staff.Branch.BusinessId;

            var sub = await _db.BusinessSubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.BusinessId == businessId, ct)
                ?? throw new KeyNotFoundException("No hay suscripción asignada.");

            // Periodo actual (anchor day del negocio)
            var nowUtc = DateTimeOffset.UtcNow;
            var (currentStart, currentEnd) = CurrentPeriod(nowUtc, sub.BillingAnchorDay);

            return new SubscriptionDto
            {
                Plan = sub.Plan.Name,
                Status = sub.Status,                                  // active | paused | cancel_requested | cancelled
                CurrentPeriodEnd = currentEnd,
                CancelAtPeriodEnd = sub.Status == "cancel_requested",
                Provider = "stripe",
                ExternalId = null // si guardas el id de sub stripe, ponlo aquí
            };
        }

        public async Task<IReadOnlyList<InvoiceListItemDto>> ListInvoicesAsync(CancellationToken ct)
        {
            var staffId = _me.StaffId ?? throw new InvalidOperationException("Usuario no autenticado.");
            var staff = await _db.Staff.Include(s => s.Branch)
                                       .FirstAsync(s => s.Id == staffId, ct);
            var businessId = staff.Branch.BusinessId;

            var usages = await _db.SubscriptionUsages
                .Where(u => u.BusinessId == businessId)
                .OrderByDescending(u => u.Year).ThenByDescending(u => u.Month)
                .Take(24)
                .ToListAsync(ct);

            var items = usages.Select(u => new InvoiceListItemDto
            {
                Id = u.Id,
                IssuedAt = PeriodEnd(u.Year, u.Month, anchorDay: 1),  // fin de mes de ese usage (ajusta si quieres el anchor real)
                Total = u.Total,
                Currency = "EUR",
                Status = u.Status switch { "charged" => "paid", "failed" => "failed", _ => "pending" },
                ExternalId = u.ProviderInvoiceId
            }).ToList();

            return items;
        }

        public async Task<(byte[] content, string fileName, string contentType)> DownloadInvoicePdfAsync(Guid invoiceId, CancellationToken ct)
        {
            // Buscar el usage y su referencia de proveedor
            var usage = await _db.SubscriptionUsages.FirstOrDefaultAsync(u => u.Id == invoiceId, ct);
            if (usage is null) throw new KeyNotFoundException("Factura no encontrada.");

            if (string.IsNullOrWhiteSpace(usage.ProviderInvoiceId))
                // Sin integración real aún → devolvemos 404-like
                throw new InvalidOperationException("No hay PDF disponible para esta factura.");

            // TODO: si integras Stripe Billing:
            //  - usar ProviderInvoiceId para pedir el PDF (o hosted_invoice_url) y proxyearlo
            //  - devolver bytes reales
            var fileName = $"invoice-{invoiceId}.pdf";
            var pdfBytes = Array.Empty<byte>();
            return (pdfBytes, fileName, "application/pdf");
        }

        public async Task RequestCancelAsync(CancelSubscriptionRequest req, CancellationToken ct)
        {
            var staffId = _me.StaffId ?? throw new InvalidOperationException("Usuario no autenticado.");
            var staff = await _db.Staff.Include(s => s.Branch).FirstAsync(s => s.Id == staffId, ct);
            var businessId = staff.Branch.BusinessId;

            var sub = await _db.BusinessSubscriptions.FirstOrDefaultAsync(s => s.BusinessId == businessId, ct)
                ?? throw new KeyNotFoundException("No hay suscripción asignada.");

            // Marcar petición de cancelación (efectiva al fin del periodo)
            if (sub.Status == "active")
                sub.Status = "cancel_requested";

            // Registrar notificación para backoffice / soporte
            var payload = new
            {
                reason = req.Reason,
                contactEmail = req.ContactEmail,
                requestedAt = DateTimeOffset.UtcNow
            };
            _db.NotificationLogs.Add(new Domain.Entities.NotificationLog
            {
                Id = Guid.NewGuid(),
                BranchId = staff.BranchId, // o una branch "principal"
                Name = "billing.cancel_request",
                Channel = "email",
                Status = "pending",
                Retries = 0,
                PayloadJson = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(payload)),
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }

        // ===== Helpers de periodo =====

        private static (DateTimeOffset start, DateTimeOffset end) CurrentPeriod(DateTimeOffset nowUtc, int anchorDay)
        {
            // Periodo = [anchor anterior, anchor actual)
            var end = AnchorFor(nowUtc, anchorDay);
            var start = end.AddMonths(-1);
            return (start, end);
        }

        private static DateTimeOffset PeriodEnd(int year, int month, int anchorDay)
        {
            // Fin del periodo del usage (usamos el primer anchor del siguiente mes)
            var end = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
            end = AnchorFor(end.AddMonths(1), anchorDay);
            return end;
        }

        private static DateTimeOffset AnchorFor(DateTimeOffset refDate, int anchorDay)
        {
            var lastDay = DateTime.DaysInMonth(refDate.Year, refDate.Month);
            var day = Math.Min(anchorDay, lastDay);
            return new DateTimeOffset(refDate.Year, refDate.Month, day, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
