using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;

namespace OreoLeads.Infrastructure.Automation.Actions;

internal interface IActionHandler
{
    Task<ActionResultDto> ExecuteAsync(string? configJson, AutomationContext context, CancellationToken ct);
}
