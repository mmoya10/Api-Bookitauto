// Infrastructure/HostedServices/BookingReminderScheduler.cs
using Microsoft.EntityFrameworkCore;
using WebApi.Infrastructure.HostedServices;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.HostedServices
{
    public sealed class BookingReminderScheduler : ScheduledBackgroundService
    {
        public BookingReminderScheduler(IServiceProvider sp)
            : base(sp, TimeSpan.FromMinutes(5)) { } // cada 5 min

        protected override async Task RunOnceAsync(CancellationToken ct)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTimeOffset.UtcNow;

            // 1) Al crear la reserva (últimos 10 min)
            var created = await db.Bookings.AsNoTracking()
                .Where(b => b.CreatedAt > now.AddMinutes(-10))
                .Select(b => new { b.Id, b.BranchId })
                .ToListAsync(ct);

            foreach (var b in created)
                await EnqueueOnceAsync(db, b.BranchId, b.Id, "booking.created", ct);

            // 2) 24h antes
            var t24 = now.AddHours(24);
            var b24 = await db.Bookings.AsNoTracking()
                .Where(b => b.Status == "confirmed" &&
                            b.StartTime >= t24.AddMinutes(-5) && b.StartTime <= t24.AddMinutes(5))
                .Select(b => new { b.Id, b.BranchId })
                .ToListAsync(ct);
            foreach (var b in b24)
                await EnqueueOnceAsync(db, b.BranchId, b.Id, "booking.reminder_24h", ct);

            // 3) 3h antes
            var t3 = now.AddHours(3);
            var b3 = await db.Bookings.AsNoTracking()
                .Where(b => b.Status == "confirmed" &&
                            b.StartTime >= t3.AddMinutes(-5) && b.StartTime <= t3.AddMinutes(5))
                .Select(b => new { b.Id, b.BranchId })
                .ToListAsync(ct);
            foreach (var b in b3)
                await EnqueueOnceAsync(db, b.BranchId, b.Id, "booking.reminder_3h", ct);

            // 4) 2h tras completed (usando UpdatedAt como marca de completion)
            var tRev = now.AddHours(-2);
            var comp = await db.Bookings.AsNoTracking()
                .Where(b => b.Status == "completed" &&
                            b.UpdatedAt >= tRev.AddMinutes(-5) && b.UpdatedAt <= tRev.AddMinutes(5))
                .Select(b => new { b.Id, b.BranchId })
                .ToListAsync(ct);
            foreach (var b in comp)
                await EnqueueOnceAsync(db, b.BranchId, b.Id, "booking.review_request", ct);

            await db.SaveChangesAsync(ct);
        }

        private static async Task EnqueueOnceAsync(AppDbContext db, Guid branchId, Guid bookingId, string name, CancellationToken ct)
        {
            var exists = await db.NotificationLogs.AnyAsync(n =>
                n.BranchId == branchId && n.BookingId == bookingId && n.Name == name, ct);
            if (exists) return;

            db.NotificationLogs.Add(new Domain.Entities.NotificationLog
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                BookingId = bookingId,
                Name = name,
                Channel = "multi", // email + push en dispatcher
                Status = "pending",
                Retries = 0,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }
}
