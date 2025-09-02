// Infrastructure/Services/Notification/NotificationConfigService.cs
using Microsoft.EntityFrameworkCore;
using WebApi.Infrastructure.Persistence;
using WebApi.Domain.Entities;

namespace WebApi.Infrastructure.Services.Notification
{
    public interface INotificationConfigService
    {
        Task<SmtpConfig> GetSmtpForBranchAsync(Guid branchId, CancellationToken ct);
    }

    public sealed class NotificationConfigService : INotificationConfigService
    {
        private readonly AppDbContext _db;
        public NotificationConfigService(AppDbContext db) => _db = db;

        public async Task<SmtpConfig> GetSmtpForBranchAsync(Guid branchId, CancellationToken ct)
        {
            var local = await _db.Set<SmtpConfig>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == branchId && x.Active, ct);
            if (local is not null) return local;

            var global = await _db.Set<SmtpConfig>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == null && x.Active, ct)
                ?? throw new InvalidOperationException("No hay SMTP configurado.");
            return global;
        }
    }
}
