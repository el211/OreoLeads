using System.Text.Json;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;

namespace OreoLeads.Infrastructure.Automation.Actions;

internal sealed class SetVariableActionHandler : IActionHandler
{
    public Task<ActionResultDto> ExecuteAsync(string? configJson, AutomationContext context, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(configJson))
                return Task.FromResult(new ActionResultDto(false, null, "No variable configuration", sw.ElapsedMilliseconds));

            using var doc = JsonDocument.Parse(configJson);
            var name = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() : null;
            var value = doc.RootElement.TryGetProperty("value", out var v) ? v.GetString() : null;

            if (string.IsNullOrWhiteSpace(name))
                return Task.FromResult(new ActionResultDto(false, null, "Variable name required", sw.ElapsedMilliseconds));

            if (value is not null) value = context.InterpolateString(value);
            context.SetVariable(name, value);

            return Task.FromResult(new ActionResultDto(true, $"Variable '{name}' set to '{value}'", null, sw.ElapsedMilliseconds));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ActionResultDto(false, null, ex.Message, sw.ElapsedMilliseconds));
        }
    }
}
