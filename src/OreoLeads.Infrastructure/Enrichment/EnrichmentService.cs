using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Enrichment.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Enrichment;

/// <summary>Lecture et validation manuelle des enrichissements (couche API).</summary>
public sealed class EnrichmentService : IEnrichmentService
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _db;
    private readonly IEnrichmentQueueService _queue;
    private readonly ICurrentUserService _currentUser;

    public EnrichmentService(
        ApplicationDbContext db,
        IEnrichmentQueueService queue,
        ICurrentUserService currentUser)
    {
        _db          = db;
        _queue       = queue;
        _currentUser = currentUser;
    }

    public async Task<EnrichmentQueueResultDto> QueueAsync(Guid leadId, bool force, CancellationToken ct = default)
    {
        var job = await _queue.QueueAsync(leadId, _currentUser.OrganizationId, force, ct);
        return new EnrichmentQueueResultDto(job.Id, job.Status.ToString());
    }

    public async Task<List<LeadEnrichmentDto>> GetByLeadAsync(Guid leadId, CancellationToken ct = default)
    {
        var jobs = await _queue.GetByLeadAsync(leadId, ct);
        return jobs.Select(ToDto).ToList();
    }

    public async Task<LeadEnrichmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var job = await _queue.GetByIdAsync(id, ct);
        return job is null ? null : ToDto(job);
    }

    public async Task<LeadEnrichmentDto?> ValidateAsync(
        Guid id, EnrichmentValidateRequestDto request, CancellationToken ct = default)
    {
        var job = await _db.LeadEnrichments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (job is null) return null;

        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == job.LeadId, ct);
        if (lead is null) return null;

        var now = DateTime.UtcNow;

        if (request.AcceptWebsite)
        {
            var website = !string.IsNullOrWhiteSpace(request.Website) ? request.Website : job.ChosenWebsiteUrl;
            if (!string.IsNullOrWhiteSpace(website))
            {
                lead.Website = website;
                lead.WebsiteValidatedAt = now;
                job.ChosenWebsiteUrl = website;
            }
        }

        if (request.AcceptEmail)
        {
            var email = !string.IsNullOrWhiteSpace(request.Email) ? request.Email : job.DiscoveredEmail;
            if (!string.IsNullOrWhiteSpace(email))
            {
                lead.Email = email;
                lead.EmailValidatedAt = now;
                job.DiscoveredEmail = email;
            }
        }

        job.ValidatedAt = now;
        job.ValidatedByUserId = Guid.TryParse(_currentUser.UserId, out var uid) ? uid : null;
        job.Status = EnrichmentStatus.Completed;
        job.SetUpdatedAt();
        lead.SetUpdatedAt();

        await _db.SaveChangesAsync(ct);
        return ToDto(job);
    }

    private static LeadEnrichmentDto ToDto(LeadEnrichment e) => new()
    {
        Id = e.Id,
        LeadId = e.LeadId,
        Status = e.Status.ToString(),
        ScheduledAt = e.ScheduledAt,
        StartedAt = e.StartedAt,
        CompletedAt = e.CompletedAt,
        AttemptCount = e.AttemptCount,
        ErrorMessage = e.ErrorMessage,
        ChosenWebsiteUrl = e.ChosenWebsiteUrl,
        WebsiteConfidence = e.WebsiteConfidence,
        MatchedSignals = Deserialize<List<string>>(e.MatchedSignalsJson) ?? new(),
        Candidates = Deserialize<List<WebsiteCandidateDto>>(e.WebsiteCandidatesJson) ?? new(),
        ExternalProfiles = Deserialize<List<ExternalProfileDto>>(e.SocialProfilesJson) ?? new(),
        AutoApplied = e.AutoApplied,
        DiscoveredEmail = e.DiscoveredEmail,
        EmailSourceUrl = e.EmailSourceUrl,
        EmailSourceType = e.EmailSourceType,
        EmailKind = e.EmailKind.ToString(),
        EmailConfidence = e.EmailConfidence,
        GuessedEmail = e.GuessedEmail,
        SearchQueriesUsed = e.SearchQueriesUsed,
        ValidatedAt = e.ValidatedAt,
        CreatedAt = e.CreatedAt,
    };

    private static T? Deserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<T>(json, JsonOpts); }
        catch { return null; }
    }
}
