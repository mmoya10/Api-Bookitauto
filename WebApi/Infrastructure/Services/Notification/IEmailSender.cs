// Infrastructure/Services/Notification/IEmailSender.cs
namespace WebApi.Infrastructure.Services.Notification
{
    public interface IEmailSender
    {
        Task SendAsync(
            Domain.Entities.SmtpConfig smtp,
            IEnumerable<string> to,
            string subject,
            string htmlBody,
            CancellationToken ct);
    }
}
