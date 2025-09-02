// Infrastructure/HostedServices/HoldSlotSweeper.cs
using Microsoft.EntityFrameworkCore;
using WebApi.Infrastructure.HostedServices;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.HostedServices
{
    public sealed class HoldSlotSweeper : ScheduledBackgroundService
    {
        public HoldSlotSweeper(IServiceProvider sp)
            : base(sp, TimeSpan.FromMinutes(2)) { } // cada 2 min

        protected override async Task RunOnceAsync(CancellationToken ct)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;

            var toExpire = await db.HoldSlots
                .Where(h => h.Status == "active" && h.ExpiresAt <= now)
                .ToListAsync(ct);

            if (toExpire.Count == 0) return;

            foreach (var h in toExpire) h.Status = "expired";
            await db.SaveChangesAsync(ct);
        }
    }
}
