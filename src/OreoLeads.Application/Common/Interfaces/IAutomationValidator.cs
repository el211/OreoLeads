using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Application.Common.Interfaces;

public interface IAutomationValidator
{
    Task<ValidationResultDto> ValidateWorkflowAsync(AutomationWorkflow workflow, CancellationToken ct = default);
    bool EvaluateConditions(string conditionsJson, AutomationContext context);
    bool HasCircularReference(Guid workflowId, string actionsJson);
    bool ExceedsDepthLimit(string actionsJson, int maxDepth = 10);
}
