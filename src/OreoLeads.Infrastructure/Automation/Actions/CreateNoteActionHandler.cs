using System.Text.Json;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Automation.Actions;

internal sealed class CreateNoteActionHandler : IActionHandler
{
    private readonly ApplicationDbContext _db;

    public CreateNoteActionHandler(ApplicationDbContext db) => _db = db;

    public async Task<ActionResultDto> ExecuteAsync(string? configJson, AutomationContext context, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (context.LeadId is null)
                return new ActionResultDto(false, null, "No LeadId in context", sw.ElapsedMilliseconds);

            var content = "Automated note";

            if (!string.IsNullOrWhiteSpace(configJson))
            {
                using var doc = JsonDocument.Parse(configJson);
                if (doc.RootElement.TryGetProperty("content", out var c))
                    content = context.InterpolateString(c.GetString() ?? content);
            }

            var note = new LeadNote
            {
                LeadId = context.LeadId.Value,
                Content = content,
                AuthorName = "Automation"
            };

            _db.LeadNotes.Add(note);
            await _db.SaveChangesAsync(ct);

            return new ActionResultDto(true, $"Note created", null, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return new ActionResultDto(false, null, ex.Message, sw.ElapsedMilliseconds);
        }
    }
}
