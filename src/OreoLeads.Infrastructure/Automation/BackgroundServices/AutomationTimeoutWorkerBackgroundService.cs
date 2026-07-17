using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Automation.BackgroundServices;

internal sealed class AutomationTimeoutWorkerBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomationTimeoutWorkerBackgroundService> _logger;

    public AutomationTimeoutWorkerBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AutomationTimeoutWorkerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutomationTimeoutWorkerBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var now = DateTime.UtcNow;
                var timedOut = await db.AutomationExecutions
                    .Where(e => e.Status == ExecutionStatus.Running && e.StartedAt != null)
                    .Include(e => e.Workflow)
                    .ToListAsync(stoppingToken);

                foreach (var execution in timedOut)
                {
                    var timeoutSeconds = execution.Workflow?.TimeoutSeconds ?? 300;
                    if (execution.StartedAt!.Value.AddSeconds(timeoutSeconds) < now)
                    {
                        execution.Status = ExecutionStatus.TimedOut;
                        execution.CompletedAt = now;
                        execution.ErrorMessage = "Execution timed out";
                        execution.SetUpdatedAt();
                        _logger.LogWarning("Execution {Id} timed out after {Seconds}s", execution.Id, timeoutSeconds);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutomationTimeoutWorkerBackgroundService tick.");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }

        _logger.LogInformation("AutomationTimeoutWorkerBackgroundService stopped.");
    }
}
