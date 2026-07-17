using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Automation.BackgroundServices;

internal sealed class AutomationSchedulerBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomationSchedulerBackgroundService> _logger;

    public AutomationSchedulerBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AutomationSchedulerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutomationSchedulerBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IAutomationScheduler>();
                var engine = scope.ServiceProvider.GetRequiredService<IAutomationEngine>();

                var dueSchedules = await scheduler.GetDueSchedulesAsync(stoppingToken);

                foreach (var schedule in dueSchedules)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        var triggerEvent = new TriggerEventDto(
                            TriggerType.Cron,
                            null,
                            schedule.OrganizationId,
                            new Dictionary<string, object?> { ["scheduleId"] = schedule.Id.ToString() });

                        await engine.ExecuteWorkflowAsync(schedule.WorkflowId, triggerEvent, stoppingToken);
                        await scheduler.UpdateNextRunAsync(schedule.Id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing scheduled workflow {WorkflowId}", schedule.WorkflowId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutomationSchedulerBackgroundService tick.");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }

        _logger.LogInformation("AutomationSchedulerBackgroundService stopped.");
    }
}
