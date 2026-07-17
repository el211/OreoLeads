using System.Text.Json;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Automation;

internal sealed class AutomationConditionEvaluator
{
    public bool Evaluate(string conditionsJson, AutomationContext ctx)
    {
        if (string.IsNullOrWhiteSpace(conditionsJson)) return true;

        try
        {
            using var doc = JsonDocument.Parse(conditionsJson);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
                return EvaluateGroup(root, ctx, LogicOperator.And);

            if (root.TryGetProperty("operator", out var op))
            {
                var logic = Enum.TryParse<LogicOperator>(op.GetString(), true, out var l) ? l : LogicOperator.And;
                if (root.TryGetProperty("conditions", out var conds))
                    return EvaluateGroup(conds, ctx, logic);
            }

            return EvaluateSingle(root, ctx);
        }
        catch
        {
            return false;
        }
    }

    private bool EvaluateGroup(JsonElement array, AutomationContext ctx, LogicOperator logic)
    {
        if (array.ValueKind != JsonValueKind.Array) return true;

        var results = new List<bool>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.TryGetProperty("conditions", out var nested))
            {
                var nestedLogic = LogicOperator.And;
                if (item.TryGetProperty("operator", out var opEl))
                    Enum.TryParse(opEl.GetString(), true, out nestedLogic);
                results.Add(EvaluateGroup(nested, ctx, nestedLogic));
            }
            else
            {
                results.Add(EvaluateSingle(item, ctx));
            }
        }

        return logic switch
        {
            LogicOperator.And => results.All(r => r),
            LogicOperator.Or => results.Any(r => r),
            LogicOperator.Not => results.Count > 0 && !results[0],
            _ => true
        };
    }

    private bool EvaluateSingle(JsonElement element, AutomationContext ctx)
    {
        var field = element.TryGetProperty("field", out var f) ? f.GetString() : null;
        var opStr = element.TryGetProperty("operator", out var o) ? o.GetString() : null;
        var value = element.TryGetProperty("value", out var v) ? v.ToString() : null;

        if (field is null || opStr is null) return true;

        if (!Enum.TryParse<ConditionOperator>(opStr, true, out var op))
            return true;

        var actual = ResolveField(field, ctx);

        return EvaluateOperator(op, actual, value);
    }

    private static string? ResolveField(string field, AutomationContext ctx)
    {
        if (ctx.Variables.TryGetValue(field, out var val))
            return val?.ToString();
        if (ctx.TriggerData.TryGetValue(field, out var triggerVal))
            return triggerVal?.ToString();
        return null;
    }

    internal static bool EvaluateOperator(ConditionOperator op, string? actual, string? expected)
    {
        return op switch
        {
            ConditionOperator.Equals => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.NotEquals => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.Contains => actual is not null && expected is not null && actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.NotContains => actual is null || expected is null || !actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.StartsWith => actual is not null && expected is not null && actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.EndsWith => actual is not null && expected is not null && actual.EndsWith(expected, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.IsNull => actual is null,
            ConditionOperator.IsNotNull => actual is not null,
            ConditionOperator.GreaterThan => CompareNumeric(actual, expected) > 0,
            ConditionOperator.LessThan => CompareNumeric(actual, expected) < 0,
            ConditionOperator.GreaterThanOrEquals => CompareNumeric(actual, expected) >= 0,
            ConditionOperator.LessThanOrEquals => CompareNumeric(actual, expected) <= 0,
            ConditionOperator.In => expected is not null && actual is not null && expected.Split(',').Select(s => s.Trim()).Contains(actual, StringComparer.OrdinalIgnoreCase),
            ConditionOperator.NotIn => expected is null || actual is null || !expected.Split(',').Select(s => s.Trim()).Contains(actual, StringComparer.OrdinalIgnoreCase),
            _ => true
        };
    }

    private static int CompareNumeric(string? a, string? b)
    {
        if (double.TryParse(a, out var da) && double.TryParse(b, out var db))
            return da.CompareTo(db);
        return string.Compare(a, b, StringComparison.Ordinal);
    }
}
