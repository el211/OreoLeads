using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Automation.BackgroundServices;

internal sealed class AutomationCleanupWorkerBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromHours(6);
    private const int RetentionDays = 90;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomationCleanupWorkerBackgroundService> _logger;

    public AutomationCleanupWorkerBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AutomationCleanupWorkerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutomationCleanupWorkerBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

                // Delete old logs
                var oldLogs = await db.AutomationExecutionLogs
                    .Where(l => l.Timestamp < cutoff)
                    .Take(1000)
                    .ToListAsync(stoppingToken);

                if (oldLogs.Count > 0)
                {
                    db.AutomationExecutionLogs.RemoveRange(oldLogs);
                    _logger.LogInformation("Cleaned up {Count} old automation logs.", oldLogs.Count);
                }

                // Delete old errors
                var oldErrors = await db.AutomationExecutionErrors
                    .Where(e => e.OccurredAt < cutoff)
                    .Take(1000)
                    .ToListAsync(stoppingToken);

                if (oldErrors.Count > 0)
                {
                    db.AutomationExecutionErrors.RemoveRange(oldErrors);
                    _logger.LogInformation("Cleaned up {Count} old automation errors.", oldErrors.Count);
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutomationCleanupWorkerBackgroundService tick.");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }

        _logger.LogInformation("AutomationCleanupWorkerBackgroundService stopped.");
    }
}
