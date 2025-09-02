// Infrastructure/HostedServices/NotificationDispatcher.cs
using Microsoft.EntityFrameworkCore;
using WebApi.Infrastructure.HostedServices;
using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Services.Notification;

namespace WebApi.Infrastructure.HostedServices
{
    public sealed class NotificationDispatcher : ScheduledBackgroundService
    {
        public NotificationDispatcher(IServiceProvider sp)
            : base(sp, TimeSpan.FromSeconds(30)) { } // cada 30s

        protected override async Task RunOnceAsync(CancellationToken ct)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cfg = scope.ServiceProvider.GetRequiredService<INotificationConfigService>();
            var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var push = scope.ServiceProvider.GetRequiredService<IPushSender>();
            var tpl  = scope.ServiceProvider.GetRequiredService<INotificationTemplateService>();

            var batch = await db.NotificationLogs
                .Where(n => n.Status == "pending")
                .OrderBy(n => n.CreatedAt)
                .Take(100)
                .ToListAsync(ct);

            foreach (var n in batch)
            {
                try
                {
                    var (subject, body) = await tpl.BuildAsync(n.BranchId, n.BookingId, n.Name, ct);
                    var (emails, tokens) = await tpl.ResolveRecipientsAsync(n.BookingId, ct);

                    // Email (si hay destinatarios)
                    if (emails.Count > 0 && n.BranchId != Guid.Empty)
                    {
                        var smtp = await cfg.GetSmtpForBranchAsync(n.BranchId, ct);
                        await email.SendAsync(smtp, emails, subject, body, ct);
                    }

                    // Push (si hay tokens)
                    if (tokens.Count > 0)
                        await push.SendAsync(tokens, subject, body, ct);

                    n.Status = "sent";
                    n.UpdatedAt = DateTimeOffset.UtcNow;
                }
                catch
                {
                    n.Status = "error";
                    n.Retries += 1;
                    n.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            if (batch.Count > 0)
                await db.SaveChangesAsync(ct);
        }
    }
}
