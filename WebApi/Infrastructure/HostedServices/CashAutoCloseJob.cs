// Infrastructure/HostedServices/CashAutoCloseJob.cs
using Microsoft.EntityFrameworkCore;
using WebApi.Infrastructure.HostedServices;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.HostedServices
{
    public sealed class CashAutoCloseJob : ScheduledBackgroundService
    {
        public CashAutoCloseJob(IServiceProvider sp)
            : base(sp, TimeSpan.FromMinutes(10)) { } // cada 10 min

        protected override async Task RunOnceAsync(CancellationToken ct)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var branches = await db.Branches.AsNoTracking()
                .Where(b => b.Status == "active")
                .Select(b => new { b.Id, b.Timezone })
                .ToListAsync(ct);

            var nowUtc = DateTimeOffset.UtcNow;

            foreach (var br in branches)
            {
                var tzid = string.IsNullOrWhiteSpace(br.Timezone) ? "Europe/Madrid" : br.Timezone;
                var tz = TimeZoneInfo.FindSystemTimeZoneById(tzid);
                var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, tz);

                // Ejecuta en la última hora del día local (23:00-23:59)
                if (nowLocal.Hour < 23) continue;

                var open = await db.CashSessions
                    .Where(s => s.BranchId == br.Id && s.ClosedAt == null)
                    .ToListAsync(ct);

                if (open.Count == 0) continue;

                foreach (var s in open)
                {
                    var sums = await db.CashMovements
                        .Where(m => m.SessionId == s.Id)
                        .GroupBy(_ => 1)
                        .Select(g => new {
                            incomes = g.Where(x => x.Type == "income").Sum(x => (decimal?)x.Total) ?? 0m,
                            expenses = g.Where(x => x.Type == "expense").Sum(x => (decimal?)x.Total) ?? 0m,
                            adjustments = g.Where(x => x.Type == "adjustment").Sum(x => (decimal?)x.Total) ?? 0m
                        })
                        .FirstOrDefaultAsync(ct) ?? new { incomes = 0m, expenses = 0m, adjustments = 0m };

                    s.ClosedAt = nowUtc;
                    s.ClosedBy = null; // system
                    s.ExpectedClose = sums.incomes - sums.expenses + sums.adjustments;
                    s.ClosingNote = "Autocierre diario";
                }

                await db.SaveChangesAsync(ct);

                // (Opcional) Encola un reporte
                // db.NotificationLogs.Add(... "cash.autoclose_report" ...);
                // await db.SaveChangesAsync(ct);
            }
        }
    }
}
