using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Application.Common.Interfaces;

public interface IAutomationExecutor
{
    Task<ActionResultDto> ExecuteActionAsync(AutomationAction action, AutomationContext context, CancellationToken ct = default);
}

public class AutomationContext
{
    public Guid WorkflowId { get; set; }
    public Guid ExecutionId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? LeadId { get; set; }
    public Dictionary<string, object?> Variables { get; set; } = new();
    public Dictionary<string, object?> TriggerData { get; set; } = new();
    public int RecursionDepth { get; set; }

    public object? GetVariable(string key) =>
        Variables.TryGetValue(key, out var value) ? value : null;

    public void SetVariable(string key, object? value) =>
        Variables[key] = value;

    public string InterpolateString(string template)
    {
        if (string.IsNullOrEmpty(template)) return template;

        var result = System.Text.RegularExpressions.Regex.Replace(template, @"\{\{([^}]+)\}\}", match =>
        {
            var key = match.Groups[1].Value.Trim();

            // Built-in variables
            if (key.Equals("Date.Now", StringComparison.OrdinalIgnoreCase))
                return DateTime.UtcNow.ToString("o");
            if (key.Equals("Execution.Id", StringComparison.OrdinalIgnoreCase))
                return ExecutionId.ToString();
            if (key.Equals("Workflow.Id", StringComparison.OrdinalIgnoreCase))
                return WorkflowId.ToString();

            // Trigger data
            if (key.StartsWith("Trigger.", StringComparison.OrdinalIgnoreCase))
            {
                var triggerKey = key["Trigger.".Length..];
                if (TriggerData.TryGetValue(triggerKey, out var triggerVal))
                    return triggerVal?.ToString() ?? string.Empty;
            }

            // User variables
            if (Variables.TryGetValue(key, out var val))
                return val?.ToString() ?? string.Empty;

            return match.Value; // Leave placeholder if not found
        });

        return result;
    }
}
