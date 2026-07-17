using System.Text.Json;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Automation.Actions;

internal sealed class CreateFollowUpActionHandler : IActionHandler
{
    private readonly ApplicationDbContext _db;

    public CreateFollowUpActionHandler(ApplicationDbContext db) => _db = db;

    public async Task<ActionResultDto> ExecuteAsync(string? configJson, AutomationContext context, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (context.LeadId is null)
                return new ActionResultDto(false, null, "No LeadId in context", sw.ElapsedMilliseconds);

            var comment = "Automated Follow-up";
            var daysDelay = 1;

            if (!string.IsNullOrWhiteSpace(configJson))
            {
                using var doc = JsonDocument.Parse(configJson);
                if (doc.RootElement.TryGetProperty("comment", out var c))
                    comment = context.InterpolateString(c.GetString() ?? comment);
                if (doc.RootElement.TryGetProperty("daysDelay", out var d))
                    daysDelay = d.GetInt32();
            }

            var followUp = new FollowUp
            {
                LeadId = context.LeadId.Value,
                Comment = comment,
                ScheduledAt = DateTime.UtcNow.AddDays(daysDelay),
                Status = FollowUpStatus.Pending,
                OrganizationId = context.OrganizationId
            };

            _db.FollowUps.Add(followUp);
            await _db.SaveChangesAsync(ct);

            return new ActionResultDto(true, $"Follow-up created", null, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return new ActionResultDto(false, null, ex.Message, sw.ElapsedMilliseconds);
        }
    }
}
