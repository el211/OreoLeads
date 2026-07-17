using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Automation.Actions;

internal sealed class AddTagActionHandler : IActionHandler
{
    private readonly ApplicationDbContext _db;

    public AddTagActionHandler(ApplicationDbContext db) => _db = db;

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

            // Find or create tag
            var tag = await _db.Tags.FirstOrDefaultAsync(tg => tg.Name == tagName, ct);
            if (tag is null)
            {
                tag = new Tag { Name = tagName };
                _db.Tags.Add(tag);
                await _db.SaveChangesAsync(ct);
            }

            // Check if already linked
            var exists = await _db.LeadTags
                .AnyAsync(lt => lt.LeadId == context.LeadId && lt.TagId == tag.Id, ct);

            if (!exists)
            {
                _db.LeadTags.Add(new LeadTag { LeadId = context.LeadId.Value, TagId = tag.Id });
                await _db.SaveChangesAsync(ct);
            }

            return new ActionResultDto(true, $"Tag '{tagName}' added", null, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return new ActionResultDto(false, null, ex.Message, sw.ElapsedMilliseconds);
        }
    }
}
