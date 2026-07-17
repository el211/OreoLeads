using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Automation.BackgroundServices;

internal sealed class AutomationRetryWorkerBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(60);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomationRetryWorkerBackgroundService> _logger;

    public AutomationRetryWorkerBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AutomationRetryWorkerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutomationRetryWorkerBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var now = DateTime.UtcNow;
                var retryItems = await db.AutomationQueueItems
                    .Where(q => q.Status == QueueItemStatus.Retrying && q.NextRetryAt != null && q.NextRetryAt <= now)
                    .ToListAsync(stoppingToken);

                foreach (var item in retryItems)
                {
                    item.Status = QueueItemStatus.Pending;
                    item.LockedBy = null;
                    item.LockedAt = null;
                    item.SetUpdatedAt();
                }

                if (retryItems.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutomationRetryWorkerBackgroundService tick.");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }

        _logger.LogInformation("AutomationRetryWorkerBackgroundService stopped.");
    }
}
