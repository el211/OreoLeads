using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Brevo;

internal sealed class SmsQueueService : ISmsQueueService
{
    private readonly ApplicationDbContext _db;

    public SmsQueueService(ApplicationDbContext db) => _db = db;

    public async Task<SmsSendJob> QueueAsync(
        Guid      leadId,
        string    toPhone,
        string?   toName,
        string    message,
        DateTime? scheduledAt,
        Guid?     organizationId,
        CancellationToken ct = default)
    {
        var job = new SmsSendJob
        {
            LeadId         = leadId,
            Status         = SmsSendStatus.Pending,
            ScheduledAt    = scheduledAt ?? DateTime.UtcNow,
            ToPhone        = toPhone,
            ToName         = toName,
            Message        = message,
            OrganizationId = organizationId
        };

        _db.Set<SmsSendJob>().Add(job);
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<List<SmsSendJob>> GetPendingAsync(int limit = 10, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.Set<SmsSendJob>()
            .Where(j =>
                j.Status == SmsSendStatus.Pending &&
                j.ScheduledAt <= now &&
                (j.NextAttemptAt == null || j.NextAttemptAt <= now))
            .OrderBy(j => j.ScheduledAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task MarkSendingAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.Status        = SmsSendStatus.Sending;
        job.LastAttemptAt = DateTime.UtcNow;
        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkSentAsync(Guid jobId, string? brevoMessageId, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.Status          = SmsSendStatus.Sent;
        job.SentAt          = DateTime.UtcNow;
        job.BrevoMessageId  = brevoMessageId;
        job.AttemptCount++;
        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(Guid jobId, string errorMessage, bool canRetry, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.AttemptCount++;
        job.ErrorMessage  = errorMessage;
        job.LastAttemptAt = DateTime.UtcNow;

        if (canRetry && job.AttemptCount < job.MaxAttempts)
        {
            var delayMinutes  = Math.Pow(2, job.AttemptCount);
            job.Status        = SmsSendStatus.Pending;
            job.NextAttemptAt = DateTime.UtcNow.AddMinutes(delayMinutes);
        }
        else
        {
            job.Status = SmsSendStatus.Failed;
        }

        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<SmsSendJob?> GetByIdAsync(Guid jobId, CancellationToken ct = default)
        => await _db.Set<SmsSendJob>().FindAsync([jobId], ct);

    public async Task<List<SmsSendJob>> GetByLeadAsync(Guid leadId, CancellationToken ct = default)
        => await _db.Set<SmsSendJob>()
                    .Where(j => j.LeadId == leadId)
                    .OrderByDescending(j => j.CreatedAt)
                    .ToListAsync(ct);

    private async Task<SmsSendJob> RequireJobAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _db.Set<SmsSendJob>().FindAsync([jobId], ct);
        return job ?? throw new InvalidOperationException($"SmsSendJob {jobId} not found.");
    }
}
