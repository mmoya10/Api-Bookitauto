using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services.Payments
{
    public interface IPaymentWebhookService
    {
        Task HandleAsync(string provider, string rawBody, IHeaderDictionary headers, CancellationToken ct);
    }

    public sealed class PaymentWebhookService : IPaymentWebhookService
    {
        private readonly AppDbContext _db;

        public PaymentWebhookService(AppDbContext db)
        {
            _db = db;
        }

        public async Task HandleAsync(string provider, string rawBody, IHeaderDictionary headers, CancellationToken ct)
        {
            switch (provider)
            {
                case "stripe":
                    await HandleStripeAsync(rawBody, headers, ct);
                    break;
                default:
                    throw new NotSupportedException($"Proveedor no soportado: {provider}");
            }
        }

        // ===== Stripe =====

        private async Task HandleStripeAsync(string rawBody, IHeaderDictionary headers, CancellationToken ct)
        {
            var sigHeader = headers["Stripe-Signature"].ToString();
            if (string.IsNullOrWhiteSpace(sigHeader))
                throw new InvalidOperationException("Falta cabecera Stripe-Signature.");

            // Parse evento (para obtener id y type)
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            var eventId = root.GetProperty("id").GetString() ?? throw new InvalidOperationException("Evento sin id.");
            var type = root.GetProperty("type").GetString() ?? "unknown";

            // Idempotencia: ¿ya procesado?
            var exists = await _db.PaymentEvents.AsNoTracking()
                .AnyAsync(x => x.Provider == "stripe" && x.EventId == eventId, ct);
            if (exists) return; // ignorar repetición

            // Buscar secret (WebhookSecret) de Stripe.
            // Si guardas config por branch, puedes inferir branch a partir del metadata más abajo.
            var webhookSecret = await GetStripeWebhookSecretAsync(ct);

            // Validar firma
            if (!VerifyStripeSignature(sigHeader, rawBody, webhookSecret, toleranceSeconds: 300))
                throw new InvalidOperationException("Firma Stripe inválida.");

            // Inserta log de evento (aún sin ProcessedAt)
            _db.PaymentEvents.Add(new Domain.Entities.PaymentEvent
            {
                Id = Guid.NewGuid(),
                Provider = "stripe",
                EventId = eventId,
                ReceivedAt = DateTimeOffset.UtcNow,
                Status = "received"
            });
            await _db.SaveChangesAsync(ct);

            // Dispatch según tipo
            switch (type)
            {
                case "payment_intent.succeeded":
                    await OnPaymentIntentSucceededAsync(root, ct);
                    break;

                case "charge.refunded":
                    await OnChargeRefundedAsync(root, ct);
                    break;

                // añade los que necesites:
                // case "payment_intent.payment_failed": ...
                default:
                    // no-op
                    break;
            }

            // marca como procesado
            var ev = await _db.PaymentEvents.FirstAsync(x => x.Provider == "stripe" && x.EventId == eventId, ct);
            ev.ProcessedAt = DateTimeOffset.UtcNow;
            ev.Status = "processed";
            await _db.SaveChangesAsync(ct);
        }

        private static bool VerifyStripeSignature(string signatureHeader, string payload, string secret, int toleranceSeconds)
        {
            // Header formato: t=timestamp,v1=signature[,v1=...,v0=...]
            var parts = signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string? ts = null;
            var v1s = new List<string>();
            foreach (var p in parts)
            {
                var kv = p.Split('=', 2);
                if (kv.Length != 2) continue;
                if (kv[0] == "t") ts = kv[1];
                if (kv[0] == "v1") v1s.Add(kv[1]);
            }
            if (ts is null || v1s.Count == 0) return false;

            var signedPayload = $"{ts}.{payload}";
            var key = Encoding.UTF8.GetBytes(secret);
            var bytes = Encoding.UTF8.GetBytes(signedPayload);

            string ComputeSig(byte[] k, byte[] b)
            {
                using var h = new HMACSHA256(k);
                var hash = h.ComputeHash(b);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }

            var expected = ComputeSig(key, bytes);

            // comparar con cualquiera de los v1
            var match = v1s.Any(v => SlowEqualsHex(expected, v));

            if (!match) return false;

            // validar tolerancia
            if (!long.TryParse(ts, out var tsNum)) return false;
            var eventTime = DateTimeOffset.FromUnixTimeSeconds(tsNum);
            var now = DateTimeOffset.UtcNow;
            return Math.Abs((now - eventTime).TotalSeconds) <= toleranceSeconds;
        }

        private static bool SlowEqualsHex(string a, string b)
        {
            // tiempo constante (aprox) para evitar timing attacks
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ char.ToLowerInvariant(b[i]);
            return diff == 0;
        }

        private async Task<string> GetStripeWebhookSecretAsync(CancellationToken ct)
        {
            // Si tienes un registro global de Stripe (platform) en PaymentProviderConfig:
            var secret = await _db.Set<Domain.Entities.PaymentProviderConfig>()
                .Where(p => p.Provider == "stripe" && p.Active)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Select(p => p.WebhookSecretEnc!)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("No hay WebhookSecret de Stripe configurado.");

            // TODO: desencriptar si procede
            return secret;
        }

        private async Task OnPaymentIntentSucceededAsync(JsonElement root, CancellationToken ct)
        {
            // data.object (PaymentIntent)
            var obj = root.GetProperty("data").GetProperty("object");

            // metadata (desde tu checkout)
            Guid? bookingId = TryGetGuid(obj, "metadata", "bookingId");
            Guid? branchId  = TryGetGuid(obj, "metadata", "branchId");

            if (bookingId is null) return;

            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId.Value, ct);
            if (booking is null) return;

            // marca como pagado online
            booking.OnlinePayment = true;
            booking.PaymentMethod ??= "online";

            // opcional: si estaba "pending", confirmar
            if (booking.Status == "pending")
                booking.Status = "confirmed";

            booking.UpdatedAt = DateTimeOffset.UtcNow;

            // Opcional: encola notificación "payment.succeeded"
            _db.NotificationLogs.Add(new Domain.Entities.NotificationLog
            {
                Id = Guid.NewGuid(),
                BranchId = branchId ?? booking.BranchId,
                BookingId = booking.Id,
                Name = "payment.succeeded",
                Channel = "multi",
                Status = "pending",
                Retries = 0,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }

        private async Task OnChargeRefundedAsync(JsonElement root, CancellationToken ct)
        {
            // data.object (Charge), puede incluir payment_intent y metadata
            var obj = root.GetProperty("data").GetProperty("object");

            Guid? bookingId = TryGetGuid(obj, "metadata", "bookingId");
            Guid? branchId  = TryGetGuid(obj, "metadata", "branchId");

            if (bookingId is null) return;

            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId.Value, ct);
            if (booking is null) return;

            // lógica mínima: marcar pago online como false (según tu política)
            booking.OnlinePayment = false;
            booking.UpdatedAt = DateTimeOffset.UtcNow;

            _db.NotificationLogs.Add(new Domain.Entities.NotificationLog
            {
                Id = Guid.NewGuid(),
                BranchId = branchId ?? booking.BranchId,
                BookingId = booking.Id,
                Name = "payment.refunded",
                Channel = "multi",
                Status = "pending",
                Retries = 0,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }

        private static Guid? TryGetGuid(JsonElement parent, string nestedProp, string key)
        {
            if (!parent.TryGetProperty(nestedProp, out var nested)) return null;
            if (nested.ValueKind != JsonValueKind.Object) return null;
            if (!nested.TryGetProperty(key, out var val)) return null;
            var s = val.GetString();
            return Guid.TryParse(s, out var g) ? g : null;
        }
    }
}
