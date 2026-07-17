using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Brevo;

internal sealed class EmailQueueService : IEmailQueueService
{
    private readonly ApplicationDbContext _db;

    public EmailQueueService(ApplicationDbContext db) => _db = db;

    public async Task<EmailSendJob> QueueAsync(
        Guid      generatedEmailId,
        Guid      leadId,
        string    toEmail,
        string?   toName,
        string    subject,
        string    htmlBody,
        DateTime? scheduledAt,
        Guid?     organizationId,
        CancellationToken ct = default)
    {
        var job = new EmailSendJob
        {
            GeneratedEmailId = generatedEmailId,
            LeadId           = leadId,
            Status           = EmailSendStatus.Pending,
            ScheduledAt      = scheduledAt ?? DateTime.UtcNow,
            ToEmail          = toEmail,
            ToName           = toName,
            Subject          = subject,
            HtmlBody         = htmlBody,
            OrganizationId   = organizationId
        };

        _db.Set<EmailSendJob>().Add(job);
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<List<EmailSendJob>> GetPendingAsync(int limit = 10, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.Set<EmailSendJob>()
            .Where(j =>
                j.Status == EmailSendStatus.Pending &&
                j.ScheduledAt <= now &&
                (j.NextAttemptAt == null || j.NextAttemptAt <= now))
            .OrderBy(j => j.ScheduledAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task MarkSendingAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.Status        = EmailSendStatus.Sending;
        job.LastAttemptAt = DateTime.UtcNow;
        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkSentAsync(Guid jobId, string? brevoMessageId, CancellationToken ct = default)
    {
        var job = await RequireJobAsync(jobId, ct);
        job.Status         = EmailSendStatus.Sent;
        job.SentAt         = DateTime.UtcNow;
        job.BrevoMessageId = brevoMessageId;
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
            // Exponential backoff: 2^AttemptCount minutes (1, 2, 4, 8…)
            var delayMinutes   = Math.Pow(2, job.AttemptCount);
            job.Status         = EmailSendStatus.Pending;
            job.NextAttemptAt  = DateTime.UtcNow.AddMinutes(delayMinutes);
        }
        else
        {
            job.Status = EmailSendStatus.Failed;
        }

        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<EmailSendJob?> GetByIdAsync(Guid jobId, CancellationToken ct = default)
        => await _db.Set<EmailSendJob>().FindAsync([jobId], ct);

    public async Task<List<EmailSendJob>> GetByLeadAsync(Guid leadId, CancellationToken ct = default)
        => await _db.Set<EmailSendJob>()
                    .Where(j => j.LeadId == leadId)
                    .OrderByDescending(j => j.CreatedAt)
                    .ToListAsync(ct);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<EmailSendJob> RequireJobAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _db.Set<EmailSendJob>().FindAsync([jobId], ct);
        return job ?? throw new InvalidOperationException($"EmailSendJob {jobId} not found.");
    }
}
