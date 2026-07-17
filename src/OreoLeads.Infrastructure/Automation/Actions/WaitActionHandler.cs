using System.Text.Json;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;

namespace OreoLeads.Infrastructure.Automation.Actions;

internal sealed class WaitActionHandler : IActionHandler
{
    public Task<ActionResultDto> ExecuteAsync(string? configJson, AutomationContext context, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Wait action marks the execution as Waiting — the actual delay is handled
        // by the queue/scheduler infrastructure, not by blocking here.
        var seconds = 60;

        if (!string.IsNullOrWhiteSpace(configJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(configJson);
                if (doc.RootElement.TryGetProperty("seconds", out var s))
                    seconds = s.GetInt32();
                else if (doc.RootElement.TryGetProperty("minutes", out var m))
                    seconds = m.GetInt32() * 60;
                else if (doc.RootElement.TryGetProperty("hours", out var h))
                    seconds = h.GetInt32() * 3600;
                else if (doc.RootElement.TryGetProperty("days", out var d))
                    seconds = d.GetInt32() * 86400;
            }
            catch { /* use default */ }
        }

        context.SetVariable("__wait_seconds", seconds);

        return Task.FromResult(new ActionResultDto(true, $"Wait {seconds}s registered", null, sw.ElapsedMilliseconds));
    }
}
