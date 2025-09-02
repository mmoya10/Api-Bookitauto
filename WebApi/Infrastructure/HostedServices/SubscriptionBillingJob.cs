using Microsoft.EntityFrameworkCore;
using WebApi.Infrastructure.HostedServices;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.HostedServices
{
    public sealed class SubscriptionBillingJob : ScheduledBackgroundService
    {
        public SubscriptionBillingJob(IServiceProvider sp)
            : base(sp, TimeSpan.FromHours(24)) { } // 1 vez/día (o cada X horas con guardas)

        protected override async Task RunOnceAsync(CancellationToken ct)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var today = DateTime.UtcNow; // ancla por UTC; si quieres TZ por business, añade tz en Business

            // Subs activas cuyo anchor es hoy (ajusta días 29-31)
            var subs = await db.BusinessSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.Status == "active")
                .ToListAsync(ct);

            foreach (var s in subs)
            {
                if (!IsAnchorToday(today, s.BillingAnchorDay)) continue;

                var (year, month, from, to) = PeriodForAnchor(today, s.BillingAnchorDay);

                // si ya existe usage → continua
                var exists = await db.SubscriptionUsages.AnyAsync(u =>
                    u.BusinessId == s.BusinessId && u.Year == year && u.Month == month, ct);
                if (exists) continue;

                // Cuenta “nuevos del periodo” por la regla
                var staffCount = await db.Staff
                    .Where(st => st.Branch.BusinessId == s.BusinessId && st.CreatedAt >= from && st.CreatedAt < to)
                    .CountAsync(ct);

                var branchCount = await db.Branches
                    .Where(br => br.BusinessId == s.BusinessId && br.CreatedAt >= from && br.CreatedAt < to)
                    .CountAsync(ct);

                var basePrice = s.Plan.BasePrice;
                var staffPrice = staffCount * s.Plan.PricePerStaff;
                var branchPrice = branchCount * s.Plan.PricePerBranch;
                var total = basePrice + staffPrice + branchPrice;

                db.SubscriptionUsages.Add(new Domain.Entities.SubscriptionUsage
                {
                    Id = Guid.NewGuid(),
                    BusinessId = s.BusinessId,
                    SubscriptionId = s.Id,
                    Year = year,
                    Month = month,
                    StaffCount = staffCount,
                    BranchCount = branchCount,
                    BasePrice = basePrice,
                    StaffPrice = staffPrice,
                    BranchPrice = branchPrice,
                    Total = total,
                    Status = "pending"
                });

                await db.SaveChangesAsync(ct);

                // TODO: Crear PaymentIntent/Invoice en Stripe por "total"
                // Guardar ProviderInvoiceId y marcar Status=charged si ok (o failed si error)
            }
        }

        private static bool IsAnchorToday(DateTime utcToday, int anchorDay)
        {
            var todayDay = DateTime.SpecifyKind(utcToday, DateTimeKind.Utc).Day;
            var lastDay = DateTime.DaysInMonth(utcToday.Year, utcToday.Month);
            var effectiveAnchor = Math.Min(anchorDay, lastDay);
            return todayDay == effectiveAnchor;
        }

        private static (int year, int month, DateTimeOffset from, DateTimeOffset to) PeriodForAnchor(DateTime utcToday, int anchorDay)
        {
            // Periodo facturado: desde anchor anterior (inclusive) hasta hoy (exclusive)
            var year = utcToday.Year;
            var month = utcToday.Month;

            var lastDay = DateTime.DaysInMonth(year, month);
            var effAnchor = Math.Min(anchorDay, lastDay);

            var to = new DateTimeOffset(year, month, effAnchor, 0, 0, 0, TimeSpan.Zero);
            var from = to.AddMonths(-1);

            // Para la primera factura (cuando StartedAt > from) puedes “prorratear” si quieres.
            return (from.Year, from.Month, from, to);
        }
    }
}
