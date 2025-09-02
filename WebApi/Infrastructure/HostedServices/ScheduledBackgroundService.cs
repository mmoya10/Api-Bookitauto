// Infrastructure/HostedServices/ScheduledBackgroundService.cs
using Microsoft.Extensions.Hosting;

namespace WebApi.Infrastructure.HostedServices
{
    public abstract class ScheduledBackgroundService : BackgroundService
    {
        private readonly TimeSpan _interval;
        protected readonly IServiceProvider Services;

        protected ScheduledBackgroundService(IServiceProvider services, TimeSpan interval)
        {
            Services = services; _interval = interval;
        }

        protected abstract Task RunOnceAsync(CancellationToken ct);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var timer = new PeriodicTimer(_interval);
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await RunOnceAsync(stoppingToken); }
                catch (Exception) { /* TODO: log */ }
                await timer.WaitForNextTickAsync(stoppingToken);
            }
        }
    }
}
