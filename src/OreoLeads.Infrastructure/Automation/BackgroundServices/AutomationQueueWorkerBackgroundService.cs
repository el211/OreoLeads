using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Automation.BackgroundServices;

internal sealed class AutomationQueueWorkerBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);
    private const int MaxJobsPerTick = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAutomationQueue _queue;
    private readonly ILogger<AutomationQueueWorkerBackgroundService> _logger;

    public AutomationQueueWorkerBackgroundService(
        IServiceScopeFactory scopeFactory,
        IAutomationQueue queue,
        ILogger<AutomationQueueWorkerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutomationQueueWorkerBackgroundService started.");
        var workerId = $"worker-{Environment.MachineName}-{Environment.ProcessId}";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                for (var i = 0; i < MaxJobsPerTick; i++)
                {
                    var item = await _queue.DequeueAsync(workerId, stoppingToken);
                    if (item is null) break;

                    try
                    {
                        await using var scope = _scopeFactory.CreateAsyncScope();
                        var engine = scope.ServiceProvider.GetRequiredService<IAutomationEngine>();

                        var triggerEvent = new TriggerEventDto(
                            TriggerType.Manual,
                            null,
                            item.OrganizationId,
                            new Dictionary<string, object?> { ["queueItemId"] = item.Id.ToString() });

                        await engine.ExecuteWorkflowAsync(item.WorkflowId, triggerEvent, stoppingToken);
                        await _queue.CompleteAsync(item.Id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing queue item {ItemId}", item.Id);
                        await _queue.FailAsync(item.Id, ex.Message, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutomationQueueWorkerBackgroundService tick.");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }

        _logger.LogInformation("AutomationQueueWorkerBackgroundService stopped.");
    }
}
