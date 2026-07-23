using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Enrichment;

internal sealed class EnrichmentQueueService : IEnrichmentQueueService
{
    private readonly ApplicationDbContext _db;

    public EnrichmentQueueService(ApplicationDbContext db) => _db = db;

    public async Task<LeadEnrichment> QueueAsync(
        Guid leadId, Guid? organizationId, bool force = false, CancellationToken ct = default)
    {
        if (!force)
        {
            var existing = await _db.LeadEnrichments
                .Where(e => e.LeadId == leadId &&
                            (e.Status == EnrichmentStatus.Pending || e.Status == EnrichmentStatus.Running))
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (existing is not null) return existing;
        }

        var job = new LeadEnrichment
        {
            LeadId         = leadId,
            OrganizationId = organizationId,
            Status         = EnrichmentStatus.Pending,
            ScheduledAt    = DateTime.UtcNow,
        };

        _db.LeadEnrichments.Add(job);
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<List<LeadEnrichment>> GetPendingAsync(int limit = 5, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.LeadEnrichments
            .Where(e =>
                e.Status == EnrichmentStatus.Pending &&
                e.ScheduledAt <= now &&
                (e.NextAttemptAt == null || e.NextAttemptAt <= now))
            .OrderBy(e => e.ScheduledAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task MarkRunningAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.Status    = EnrichmentStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkCompletedAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.Status      = EnrichmentStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;
        job.AttemptCount++;
        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkNeedsReviewAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.Status      = EnrichmentStatus.NeedsReview;
        job.CompletedAt = DateTime.UtcNow;
        job.AttemptCount++;
        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(Guid jobId, string errorMessage, bool canRetry, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.AttemptCount++;
        job.ErrorMessage = errorMessage;

        if (canRetry && job.AttemptCount < job.MaxAttempts)
        {
            // Backoff exponentiel : 2^AttemptCount minutes (2, 4, 8…)
            var delayMinutes  = Math.Pow(2, job.AttemptCount);
            job.Status        = EnrichmentStatus.Pending;
            job.NextAttemptAt = DateTime.UtcNow.AddMinutes(delayMinutes);
        }
        else
        {
            job.Status      = EnrichmentStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
        }

        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<LeadEnrichment?> GetByIdAsync(Guid jobId, CancellationToken ct = default)
        => await _db.LeadEnrichments.FindAsync([jobId], ct);

    public async Task<List<LeadEnrichment>> GetByLeadAsync(Guid leadId, CancellationToken ct = default)
        => await _db.LeadEnrichments
            .Where(e => e.LeadId == leadId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

    private async Task<LeadEnrichment> RequireJobAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _db.LeadEnrichments.FindAsync([jobId], ct);
        return job ?? throw new InvalidOperationException($"LeadEnrichment {jobId} not found.");
    }
}
