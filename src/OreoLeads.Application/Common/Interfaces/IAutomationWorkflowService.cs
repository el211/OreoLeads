using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Application.Common.Interfaces;

public interface IAutomationWorkflowService
{
    // CRUD
    Task<List<WorkflowSummaryDto>> GetWorkflowsAsync(Guid? organizationId, CancellationToken ct = default);
    Task<AutomationWorkflow?> GetWorkflowAsync(Guid id, CancellationToken ct = default);
    Task<AutomationWorkflow> CreateWorkflowAsync(CreateAutomationWorkflowDto dto, Guid? organizationId, CancellationToken ct = default);
    Task<AutomationWorkflow> UpdateWorkflowAsync(Guid id, UpdateAutomationWorkflowDto dto, CancellationToken ct = default);
    Task DeleteWorkflowAsync(Guid id, CancellationToken ct = default);

    // Lifecycle
    Task ActivateAsync(Guid id, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
    Task PauseAsync(Guid id, CancellationToken ct = default);
    Task ResumeAsync(Guid id, CancellationToken ct = default);

    // Clone / Import / Export
    Task<AutomationWorkflow> CloneWorkflowAsync(Guid id, CancellationToken ct = default);
    Task<string> ExportWorkflowAsync(Guid id, CancellationToken ct = default);
    Task<AutomationWorkflow> ImportWorkflowAsync(string json, Guid? organizationId, CancellationToken ct = default);

    // Versions
    Task<List<AutomationVersion>> GetVersionsAsync(Guid workflowId, CancellationToken ct = default);

    // Templates
    Task<List<AutomationTemplate>> GetTemplatesAsync(CancellationToken ct = default);
    Task<AutomationWorkflow> UseTemplateAsync(Guid templateId, Guid? organizationId, CancellationToken ct = default);
    Task SeedBuiltInTemplatesAsync(CancellationToken ct = default);

    // Executions
    Task<List<ExecutionSummaryDto>> GetExecutionsAsync(Guid? workflowId, Guid? organizationId, CancellationToken ct = default);
    Task<AutomationExecution?> GetExecutionAsync(Guid executionId, CancellationToken ct = default);
    Task<List<AutomationExecutionLog>> GetExecutionLogsAsync(Guid executionId, CancellationToken ct = default);

    // Monitoring
    Task<MonitoringStatsDto> GetMonitoringStatsAsync(Guid? organizationId, CancellationToken ct = default);
}
