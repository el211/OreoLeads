using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Automation.Actions;

internal sealed class RemoveTagActionHandler : IActionHandler
{
    private readonly ApplicationDbContext _db;

    public RemoveTagActionHandler(ApplicationDbContext db) => _db = db;

    public async Task<ActionResultDto> ExecuteAsync(string? configJson, AutomationContext context, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (context.LeadId is null)
                return new ActionResultDto(false, null, "No LeadId in context", sw.ElapsedMilliseconds);

            if (string.IsNullOrWhiteSpace(configJson))
                return new ActionResultDto(false, null, "No tag configuration", sw.ElapsedMilliseconds);

            using var doc = JsonDocument.Parse(configJson);
            var tagName = doc.RootElement.TryGetProperty("tag", out var t) ? t.GetString() : null;
            if (string.IsNullOrWhiteSpace(tagName))
                return new ActionResultDto(false, null, "Tag name required", sw.ElapsedMilliseconds);

            tagName = context.InterpolateString(tagName);

            var tag = await _db.Tags.FirstOrDefaultAsync(tg => tg.Name == tagName, ct);
            if (tag is null)
                return new ActionResultDto(true, $"Tag '{tagName}' not found, nothing to remove", null, sw.ElapsedMilliseconds);

            var link = await _db.LeadTags
                .FirstOrDefaultAsync(lt => lt.LeadId == context.LeadId && lt.TagId == tag.Id, ct);

            if (link is not null)
            {
                _db.LeadTags.Remove(link);
                await _db.SaveChangesAsync(ct);
            }

            return new ActionResultDto(true, $"Tag '{tagName}' removed", null, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return new ActionResultDto(false, null, ex.Message, sw.ElapsedMilliseconds);
        }
    }
}
