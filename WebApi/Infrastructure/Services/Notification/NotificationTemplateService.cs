// Infrastructure/Services/Notification/NotificationTemplateService.cs
using Microsoft.EntityFrameworkCore;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services.Notification
{
    public interface INotificationTemplateService
    {
        Task<(string subject, string html)> BuildAsync(Guid? branchId, Guid? bookingId, string name, CancellationToken ct);
        Task<(IReadOnlyList<string> emails, IReadOnlyList<string> deviceTokens)> ResolveRecipientsAsync(Guid? bookingId, CancellationToken ct);
    }

    public sealed class NotificationTemplateService : INotificationTemplateService
    {
        private readonly AppDbContext _db;
        public NotificationTemplateService(AppDbContext db) => _db = db;

        public async Task<(string subject, string html)> BuildAsync(Guid? branchId, Guid? bookingId, string name, CancellationToken ct)
        {
            string subject = name switch
            {
                "booking.created"        => "Reserva confirmada",
                "booking.reminder_24h"   => "Recordatorio: tu cita es mañana",
                "booking.reminder_3h"    => "Recordatorio: tu cita es en 3 horas",
                "booking.review_request" => "¿Nos dejas tu reseña?",
                "cash.autoclose_report"  => "Cierre de caja automático",
                _ => "Notificación"
            };

            var html = "<p>Notificación</p>";
            if (bookingId is Guid bid)
            {
                var b = await _db.Bookings
                    .Include(x => x.Branch).Include(x => x.Service)
                    .AsNoTracking().FirstOrDefaultAsync(x => x.Id == bid, ct);
                if (b is not null)
                {
                    html = $"""
                        <h3>{subject}</h3>
                        <p><b>Negocio:</b> {b.Branch.Name}</p>
                        <p><b>Servicio:</b> {b.Service.Name}</p>
                        <p><b>Inicio:</b> {b.StartTime:yyyy-MM-dd HH:mm}</p>
                        """;
                }
            }
            return (subject, html);
        }

        public async Task<(IReadOnlyList<string> emails, IReadOnlyList<string> deviceTokens)> ResolveRecipientsAsync(Guid? bookingId, CancellationToken ct)
        {
            var emails = new List<string>();
            var tokens = new List<string>();

            if (bookingId is Guid bid)
            {
                var b = await _db.Bookings
                    .Include(x => x.User)
                    .AsNoTracking().FirstOrDefaultAsync(x => x.Id == bid, ct);
                if (b?.User?.Email is string email && !string.IsNullOrWhiteSpace(email))
                    emails.Add(email);

                if (b?.UserId is Guid uid)
                {
                    var devs = await _db.Set<Domain.Entities.UserDevice>().AsNoTracking()
                        .Where(d => d.UserId == uid && d.NotificationsEnabled)
                        .Select(d => d.DeviceToken)
                        .ToListAsync(ct);
                    tokens.AddRange(devs);
                }
            }
            return (emails, tokens);
        }
    }
}
