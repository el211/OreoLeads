using System.Text.Json;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Automation.Actions;

internal sealed class ExecuteWorkflowActionHandler : IActionHandler
{
    private readonly IAutomationEngine _engine;

    public ExecuteWorkflowActionHandler(IAutomationEngine engine) => _engine = engine;

    public async Task<ActionResultDto> ExecuteAsync(string? configJson, AutomationContext context, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (context.RecursionDepth >= 10)
                return new ActionResultDto(false, null, "Max recursion depth (10) exceeded", sw.ElapsedMilliseconds);

            if (string.IsNullOrWhiteSpace(configJson))
                return new ActionResultDto(false, null, "No workflow configuration", sw.ElapsedMilliseconds);

            using var doc = JsonDocument.Parse(configJson);
            var workflowIdStr = doc.RootElement.TryGetProperty("workflowId", out var w) ? w.GetString() : null;

            if (workflowIdStr is null || !Guid.TryParse(workflowIdStr, out var workflowId))
                return new ActionResultDto(false, null, "Invalid workflowId", sw.ElapsedMilliseconds);

            // Detect direct self-reference
            if (workflowId == context.WorkflowId)
                return new ActionResultDto(false, null, "Cannot execute self (circular reference)", sw.ElapsedMilliseconds);

            var triggerEvent = new TriggerEventDto(
                TriggerType.Manual,
                context.LeadId,
                context.OrganizationId,
                new Dictionary<string, object?>(context.Variables));

            var result = await _engine.ExecuteWorkflowAsync(workflowId, triggerEvent, ct);

            return new ActionResultDto(result.Success, result.Message, result.Success ? null : string.Join("; ", result.Errors), sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return new ActionResultDto(false, null, ex.Message, sw.ElapsedMilliseconds);
        }
    }
}
