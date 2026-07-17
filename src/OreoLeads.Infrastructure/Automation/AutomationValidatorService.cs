using System.Text.Json;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Entities.Automation;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Automation;

internal sealed class AutomationValidatorService : IAutomationValidator
{
    private readonly AutomationConditionEvaluator _evaluator = new();

    public Task<ValidationResultDto> ValidateWorkflowAsync(AutomationWorkflow workflow, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(workflow.Name))
            errors.Add("Workflow name is required");

        if (workflow.TimeoutSeconds <= 0)
            errors.Add("Timeout must be positive");

        if (workflow.ConcurrencyLimit <= 0)
            errors.Add("Concurrency limit must be positive");

        // Validate actions JSON
        if (!string.IsNullOrWhiteSpace(workflow.ActionsJson))
        {
            try
            {
                JsonDocument.Parse(workflow.ActionsJson);

                if (HasCircularReference(workflow.Id, workflow.ActionsJson))
                    errors.Add("Workflow contains circular references");

                if (ExceedsDepthLimit(workflow.ActionsJson))
                    warnings.Add("Workflow actions exceed recommended nesting depth");
            }
            catch (JsonException)
            {
                errors.Add("Actions JSON is malformed");
            }
        }

        // Validate trigger JSON
        if (!string.IsNullOrWhiteSpace(workflow.TriggerJson))
        {
            try { JsonDocument.Parse(workflow.TriggerJson); }
            catch (JsonException) { errors.Add("Trigger JSON is malformed"); }
        }

        return Task.FromResult(new ValidationResultDto(errors.Count == 0, errors, warnings));
    }

    public bool EvaluateConditions(string conditionsJson, AutomationContext context)
    {
        return _evaluator.Evaluate(conditionsJson, context);
    }

    public bool HasCircularReference(Guid workflowId, string actionsJson)
    {
        if (string.IsNullOrWhiteSpace(actionsJson)) return false;

        try
        {
            using var doc = JsonDocument.Parse(actionsJson);
            var referencedIds = new HashSet<Guid>();
            CollectWorkflowReferences(doc.RootElement, referencedIds);
            return referencedIds.Contains(workflowId);
        }
        catch
        {
            return false;
        }
    }

    public bool ExceedsDepthLimit(string actionsJson, int maxDepth = 10)
    {
        if (string.IsNullOrWhiteSpace(actionsJson)) return false;

        try
        {
            using var doc = JsonDocument.Parse(actionsJson);
            return GetDepth(doc.RootElement) > maxDepth;
        }
        catch
        {
            return false;
        }
    }

    private static void CollectWorkflowReferences(JsonElement element, HashSet<Guid> ids)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                CollectWorkflowReferences(item, ids);
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("type", out var typeEl) &&
                typeEl.GetString() == nameof(ActionType.ExecuteWorkflow) &&
                element.TryGetProperty("config", out var config) &&
                config.TryGetProperty("workflowId", out var wfId))
            {
                if (Guid.TryParse(wfId.GetString(), out var id))
                    ids.Add(id);
            }

            foreach (var prop in element.EnumerateObject())
                CollectWorkflowReferences(prop.Value, ids);
        }
    }

    private static int GetDepth(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            int max = 0;
            foreach (var item in element.EnumerateArray())
                max = Math.Max(max, GetDepth(item));
            return max + 1;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            int max = 0;
            foreach (var prop in element.EnumerateObject())
                max = Math.Max(max, GetDepth(prop.Value));
            return max + 1;
        }

        return 1;
    }
}
