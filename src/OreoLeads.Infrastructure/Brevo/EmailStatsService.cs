using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Brevo.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Brevo;

internal sealed class EmailStatsService : IEmailStatsService
{
    private readonly ApplicationDbContext _db;

    public EmailStatsService(ApplicationDbContext db) => _db = db;

    public async Task<EmailStatsDto> GetStatsAsync(
        Guid?     organizationId = null,
        DateTime? from           = null,
        DateTime? to             = null,
        CancellationToken ct     = default)
    {
        // ── Jobs query ────────────────────────────────────────────────────────
        var jobsQuery = _db.Set<EmailSendJob>().AsQueryable();

        if (organizationId.HasValue)
            jobsQuery = jobsQuery.Where(j => j.OrganizationId == organizationId);

        if (from.HasValue)
            jobsQuery = jobsQuery.Where(j => j.CreatedAt >= from.Value);

        if (to.HasValue)
            jobsQuery = jobsQuery.Where(j => j.CreatedAt <= to.Value);

        var totalSent = await jobsQuery
            .CountAsync(j => j.Status == EmailSendStatus.Sent, ct);

        // Collect job ids for event correlation
        var jobIds = await jobsQuery
            .Select(j => (Guid?)j.Id)
            .ToListAsync(ct);

        // ── Events query ──────────────────────────────────────────────────────
        var eventsQuery = _db.Set<EmailEvent>().AsQueryable();

        if (jobIds.Count > 0)
            eventsQuery = eventsQuery.Where(e => e.EmailSendJobId != null && jobIds.Contains(e.EmailSendJobId));

        if (from.HasValue)
            eventsQuery = eventsQuery.Where(e => e.OccurredAt >= from.Value);

        if (to.HasValue)
            eventsQuery = eventsQuery.Where(e => e.OccurredAt <= to.Value);

        var eventCounts = await eventsQuery
            .GroupBy(e => e.EventType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Get(EmailEventType type) =>
            eventCounts.FirstOrDefault(e => e.Type == type)?.Count ?? 0;

        var totalDelivered   = Get(EmailEventType.Delivered);
        var totalOpened      = Get(EmailEventType.Opened);
        var totalClicked     = Get(EmailEventType.Clicked);
        var totalBounced     = Get(EmailEventType.SoftBounce) + Get(EmailEventType.HardBounce);
        var totalSpam        = Get(EmailEventType.Spam);
        var totalUnsubscribed = Get(EmailEventType.Unsubscribed);
        var replyCount       = Get(EmailEventType.Reply);

        var openRate   = totalSent > 0 ? Math.Round(totalOpened   / (double)totalSent * 100, 2) : 0d;
        var clickRate  = totalSent > 0 ? Math.Round(totalClicked  / (double)totalSent * 100, 2) : 0d;
        var bounceRate = totalSent > 0 ? Math.Round(totalBounced  / (double)totalSent * 100, 2) : 0d;

        return new EmailStatsDto(
            TotalSent:         totalSent,
            TotalDelivered:    totalDelivered,
            TotalOpened:       totalOpened,
            TotalClicked:      totalClicked,
            TotalBounced:      totalBounced,
            TotalSpam:         totalSpam,
            TotalUnsubscribed: totalUnsubscribed,
            OpenRate:          openRate,
            ClickRate:         clickRate,
            BounceRate:        bounceRate,
            ReplyCount:        replyCount
        );
    }
}
