using OreoLeads.Application.Features.Automation.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface IAutomationEngine
{
    Task<AutomationExecutionResultDto> TriggerAsync(TriggerEventDto triggerEvent, CancellationToken ct = default);
    Task<AutomationExecutionResultDto> ExecuteWorkflowAsync(Guid workflowId, TriggerEventDto triggerEvent, CancellationToken ct = default);
    Task CancelExecutionAsync(Guid executionId, CancellationToken ct = default);
    Task RetryExecutionAsync(Guid executionId, CancellationToken ct = default);
}
