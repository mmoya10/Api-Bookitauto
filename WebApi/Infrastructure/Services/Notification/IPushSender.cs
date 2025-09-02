// Infrastructure/Services/Notification/IPushSender.cs
namespace WebApi.Infrastructure.Services.Notification
{
    public interface IPushSender
    {
        Task SendAsync(IEnumerable<string> deviceTokens, string title, string body, CancellationToken ct);
    }

    public sealed class NoopPushSender : IPushSender
    {
        public Task SendAsync(IEnumerable<string> deviceTokens, string title, string body, CancellationToken ct)
            => Task.CompletedTask; // TODO: integrar FCM/APNs
    }
}
