using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Automation.Actions;

internal sealed class ChangeStatusActionHandler : IActionHandler
{
    private readonly ApplicationDbContext _db;

    public ChangeStatusActionHandler(ApplicationDbContext db) => _db = db;

    public async Task<ActionResultDto> ExecuteAsync(string? configJson, AutomationContext context, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (context.LeadId is null)
                return new ActionResultDto(false, null, "No LeadId in context", sw.ElapsedMilliseconds);
            if (string.IsNullOrWhiteSpace(configJson))
                return new ActionResultDto(false, null, "No status configuration", sw.ElapsedMilliseconds);

            using var doc = JsonDocument.Parse(configJson);
            var statusStr = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;

            if (statusStr is null || !Enum.TryParse<LeadStatus>(statusStr, true, out var newStatus))
                return new ActionResultDto(false, null, $"Invalid status: {statusStr}", sw.ElapsedMilliseconds);

            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == context.LeadId, ct);
            if (lead is null)
                return new ActionResultDto(false, null, $"Lead {context.LeadId} not found", sw.ElapsedMilliseconds);

            lead.Status = newStatus;
            lead.SetUpdatedAt();
            await _db.SaveChangesAsync(ct);

            return new ActionResultDto(true, $"Status changed to {newStatus}", null, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return new ActionResultDto(false, null, ex.Message, sw.ElapsedMilliseconds);
        }
    }
}
