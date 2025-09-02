// Infrastructure/Services/Notification/SmtpEmailSender.cs
using System.Net;
using System.Net.Mail;

namespace WebApi.Infrastructure.Services.Notification
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        public async Task SendAsync(Domain.Entities.SmtpConfig smtp, IEnumerable<string> to, string subject, string htmlBody, CancellationToken ct)
        {
            using var client = new SmtpClient(smtp.Host, smtp.Port)
            {
                EnableSsl = smtp.UseSsl,
                Credentials = new NetworkCredential(smtp.Username, smtp.PasswordEnc) // TODO: desencriptar si procede
            };

            using var msg = new MailMessage
            {
                From = new MailAddress(smtp.FromEmail, smtp.FromName ?? smtp.FromEmail),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            foreach (var addr in to.Distinct())
                msg.To.Add(addr);

            // SmtpClient no soporta CancellationToken; respétalo manualmente si quieres con timeout.
            await client.SendMailAsync(msg);
        }
    }
}
