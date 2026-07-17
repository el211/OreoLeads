using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Analytics;

internal sealed class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AnalyticsService> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public AnalyticsService(ApplicationDbContext db, IMemoryCache cache, ILogger<AnalyticsService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    // ── Executive Dashboard ──────────────────────────────────────────────────

    public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default)
    {
        var cacheKey = $"analytics:dashboard:{orgId}:{range.Preset}";
        if (_cache.TryGetValue(cacheKey, out ExecutiveDashboardDto? cached))
            return cached!;

        var (start, end) = range.Resolve();
        var now = DateTime.UtcNow;

        var leadQuery = _db.Leads.AsQueryable();
        var emailQuery = _db.EmailSendJobs.AsQueryable();
        var execQuery = _db.AutomationExecutions.AsQueryable();
        var syncQuery = _db.AirtableSyncJobs.AsQueryable();

        // Lead stats
        var leadsInRange = leadQuery.Where(l => l.CreatedAt >= start && l.CreatedAt <= end);
        var today = await leadsInRange.CountAsync(l => l.CreatedAt >= now.Date, ct);
        var thisWeek = await leadsInRange.CountAsync(l => l.CreatedAt >= now.Date.AddDays(-(int)now.DayOfWeek), ct);
        var thisMonth = await leadsInRange.CountAsync(l => l.CreatedAt >= new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), ct);
        var thisYear = await leadsInRange.CountAsync(l => l.CreatedAt >= new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), ct);
        var totalInRange = await leadsInRange.CountAsync(ct);
        var newProspects = await leadsInRange.CountAsync(l => l.Status == LeadStatus.New, ct);
        var converted = await leadsInRange.CountAsync(l => l.Status == LeadStatus.Client, ct);
        var conversionRate = totalInRange > 0 ? (double)converted / totalInRange * 100 : 0;

        var leads = new LeadStatsDto(today, thisWeek, thisMonth, thisYear, newProspects, converted, Math.Round(conversionRate, 2));

        // Email stats
        var emailsInRange = emailQuery.Where(e => e.CreatedAt >= start && e.CreatedAt <= end);
        var sent = await emailsInRange.CountAsync(e => e.Status == EmailSendStatus.Sent, ct);

        // Email events
        var eventQuery = _db.EmailEvents.AsQueryable();
        var eventsInRange = eventQuery.Where(ev => ev.OccurredAt >= start && ev.OccurredAt <= end);
        var delivered = await eventsInRange.CountAsync(ev => ev.EventType == EmailEventType.Delivered, ct);
        var opened = await eventsInRange.CountAsync(ev => ev.EventType == EmailEventType.Opened, ct);
        var clicked = await eventsInRange.CountAsync(ev => ev.EventType == EmailEventType.Clicked, ct);
        var replied = await eventsInRange.CountAsync(ev => ev.EventType == EmailEventType.Reply, ct);
        var bounced = await eventsInRange.CountAsync(ev => ev.EventType == EmailEventType.HardBounce || ev.EventType == EmailEventType.SoftBounce, ct);
        var unsub = await eventsInRange.CountAsync(ev => ev.EventType == EmailEventType.Unsubscribed, ct);

        var openRate = sent > 0 ? (double)opened / sent * 100 : 0;
        var clickRate = sent > 0 ? (double)clicked / sent * 100 : 0;
        var replyRate = sent > 0 ? (double)replied / sent * 100 : 0;
        var bounceRate = sent > 0 ? (double)bounced / sent * 100 : 0;

        var emails = new EmailStatsDto(sent, delivered, opened, clicked, replied, bounced, unsub,
            Math.Round(openRate, 2), Math.Round(clickRate, 2), Math.Round(replyRate, 2), Math.Round(bounceRate, 2));

        // Automation stats
        var execsInRange = execQuery.Where(e => e.CreatedAt >= start && e.CreatedAt <= end);
        var totalExecs = await execsInRange.CountAsync(ct);
        var successExecs = await execsInRange.CountAsync(e => e.Status == ExecutionStatus.Completed, ct);
        var failedExecs = await execsInRange.CountAsync(e => e.Status == ExecutionStatus.Failed, ct);
        var retriedExecs = await execsInRange.CountAsync(e => e.RetryCount > 0, ct);
        var successRate = totalExecs > 0 ? (double)successExecs / totalExecs * 100 : 0;
        var avgDuration = totalExecs > 0
            ? await execsInRange.Where(e => e.DurationMs.HasValue).Select(e => (double)e.DurationMs!.Value).DefaultIfEmpty(0).AverageAsync(ct)
            : 0;

        var automation = new AutomationStatsDto(totalExecs, successExecs, failedExecs, retriedExecs,
            Math.Round(successRate, 2), Math.Round(avgDuration, 2));

        // Airtable stats
        var syncsInRange = syncQuery.Where(s => s.CreatedAt >= start && s.CreatedAt <= end);
        var totalSyncs = await syncsInRange.CountAsync(ct);
        var successSyncs = await syncsInRange.CountAsync(s => s.Status == AirtableSyncJobStatus.Completed, ct);
        var failedSyncs = await syncsInRange.CountAsync(s => s.Status == AirtableSyncJobStatus.Failed, ct);
        var conflictSyncs = await syncsInRange.SumAsync(s => s.ConflictRecords, ct);
        var syncSuccessRate = totalSyncs > 0 ? (double)successSyncs / totalSyncs * 100 : 0;

        var airtable = new AirtableStatsDto(totalSyncs, successSyncs, failedSyncs, conflictSyncs,
            Math.Round(syncSuccessRate, 2));

        // Pending follow-ups
        var pendingFollowUps = await _db.FollowUps.CountAsync(f => f.Status == FollowUpStatus.Pending, ct);

        // User activity (top 5)
        var userActivityRaw = await _db.LeadActivities
            .Where(a => a.CreatedAt >= start && a.CreatedAt <= end && a.UserId.HasValue)
            .GroupBy(a => a.UserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(ct);

        var userActivity = userActivityRaw
            .Select(x => new TopUserActivityDto(x.UserId.ToString(), null, x.Count))
            .ToList();

        var result = new ExecutiveDashboardDto(leads, emails, automation, airtable, pendingFollowUps, userActivity, DateTime.UtcNow);
        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    // ── KPI Summary ──────────────────────────────────────────────────────────

    public async Task<KpiSummaryDto> GetKpiSummaryAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default)
    {
        var cacheKey = $"analytics:kpi:{orgId}:{range.Preset}";
        if (_cache.TryGetValue(cacheKey, out KpiSummaryDto? cached))
            return cached!;

        var (start, end) = range.Resolve();
        var days = Math.Max((end - start).TotalDays, 1);

        // Leads
        var leadsInRange = _db.Leads.Where(l => l.CreatedAt >= start && l.CreatedAt <= end);
        var totalLeads = await leadsInRange.CountAsync(ct);
        var converted = await leadsInRange.CountAsync(l => l.Status == LeadStatus.Client, ct);
        var conversionRate = totalLeads > 0 ? (double)converted / totalLeads * 100 : 0;
        var leadsPerDay = totalLeads / days;

        // Emails
        var sentEmails = await _db.EmailSendJobs.CountAsync(e => e.CreatedAt >= start && e.CreatedAt <= end && e.Status == EmailSendStatus.Sent, ct);
        var emailsPerDay = sentEmails / days;

        var events = _db.EmailEvents.Where(ev => ev.OccurredAt >= start && ev.OccurredAt <= end);
        var opened = await events.CountAsync(ev => ev.EventType == EmailEventType.Opened, ct);
        var clicked = await events.CountAsync(ev => ev.EventType == EmailEventType.Clicked, ct);
        var replied = await events.CountAsync(ev => ev.EventType == EmailEventType.Reply, ct);
        var bounced = await events.CountAsync(ev => ev.EventType == EmailEventType.HardBounce || ev.EventType == EmailEventType.SoftBounce, ct);

        var openRate = sentEmails > 0 ? (double)opened / sentEmails * 100 : 0;
        var clickRate = sentEmails > 0 ? (double)clicked / sentEmails * 100 : 0;
        var replyRate = sentEmails > 0 ? (double)replied / sentEmails * 100 : 0;
        var bounceRate = sentEmails > 0 ? (double)bounced / sentEmails * 100 : 0;

        // Automation
        var execs = _db.AutomationExecutions.Where(e => e.CreatedAt >= start && e.CreatedAt <= end);
        var totalExecs = await execs.CountAsync(ct);
        var successExecs = await execs.CountAsync(e => e.Status == ExecutionStatus.Completed, ct);
        var failedExecs = await execs.CountAsync(e => e.Status == ExecutionStatus.Failed, ct);
        var retriedExecs = await execs.CountAsync(e => e.RetryCount > 0, ct);
        var autoSuccessRate = totalExecs > 0 ? (double)successExecs / totalExecs * 100 : 0;
        var autoFailureRate = totalExecs > 0 ? (double)failedExecs / totalExecs * 100 : 0;
        var autoRetryRate = totalExecs > 0 ? (double)retriedExecs / totalExecs * 100 : 0;
        var workflowsPerDay = totalExecs / days;

        // Airtable
        var syncs = _db.AirtableSyncJobs.Where(s => s.CreatedAt >= start && s.CreatedAt <= end);
        var totalSyncs = await syncs.CountAsync(ct);
        var successSyncs = await syncs.CountAsync(s => s.Status == AirtableSyncJobStatus.Completed, ct);
        var airtableSyncSuccess = totalSyncs > 0 ? (double)successSyncs / totalSyncs * 100 : 0;

        // Lead velocity = new leads last 7 days vs previous 7 days
        var recentLeads = await _db.Leads.CountAsync(l => l.CreatedAt >= DateTime.UtcNow.AddDays(-7), ct);
        var previousLeads = await _db.Leads.CountAsync(l => l.CreatedAt >= DateTime.UtcNow.AddDays(-14) && l.CreatedAt < DateTime.UtcNow.AddDays(-7), ct);
        var leadVelocity = previousLeads > 0 ? ((double)recentLeads - previousLeads) / previousLeads * 100 : 0;

        var result = new KpiSummaryDto(
            Math.Round(conversionRate, 2), Math.Round(replyRate, 2), Math.Round(openRate, 2),
            Math.Round(clickRate, 2), Math.Round(bounceRate, 2), Math.Round(leadVelocity, 2),
            0, 0, // average response/conversion time - would need more data
            Math.Round(autoSuccessRate, 2), Math.Round(autoFailureRate, 2), Math.Round(autoRetryRate, 2),
            Math.Round(airtableSyncSuccess, 2),
            Math.Round(emailsPerDay, 2), Math.Round(leadsPerDay, 2), Math.Round(workflowsPerDay, 2));

        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    // ── Email Analytics ──────────────────────────────────────────────────────

    public async Task<EmailAnalyticsDto> GetEmailAnalyticsAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default)
    {
        var cacheKey = $"analytics:email:{orgId}:{range.Preset}";
        if (_cache.TryGetValue(cacheKey, out EmailAnalyticsDto? cached))
            return cached!;

        var (start, end) = range.Resolve();

        var sentEmails = await _db.EmailSendJobs.CountAsync(e => e.CreatedAt >= start && e.CreatedAt <= end && e.Status == EmailSendStatus.Sent, ct);
        var events = _db.EmailEvents.Where(ev => ev.OccurredAt >= start && ev.OccurredAt <= end);

        var opened = await events.CountAsync(ev => ev.EventType == EmailEventType.Opened, ct);
        var clicked = await events.CountAsync(ev => ev.EventType == EmailEventType.Clicked, ct);
        var replied = await events.CountAsync(ev => ev.EventType == EmailEventType.Reply, ct);
        var bounced = await events.CountAsync(ev => ev.EventType == EmailEventType.HardBounce || ev.EventType == EmailEventType.SoftBounce, ct);
        var spam = await events.CountAsync(ev => ev.EventType == EmailEventType.Spam, ct);
        var unsub = await events.CountAsync(ev => ev.EventType == EmailEventType.Unsubscribed, ct);

        var openRate = sentEmails > 0 ? (double)opened / sentEmails * 100 : 0;
        var clickRate = sentEmails > 0 ? (double)clicked / sentEmails * 100 : 0;
        var replyRate = sentEmails > 0 ? (double)replied / sentEmails * 100 : 0;
        var bounceRate = sentEmails > 0 ? (double)bounced / sentEmails * 100 : 0;
        var spamRate = sentEmails > 0 ? (double)spam / sentEmails * 100 : 0;
        var unsubRate = sentEmails > 0 ? (double)unsub / sentEmails * 100 : 0;

        // Daily stats
        var dailyStats = await _db.EmailSendJobs
            .Where(e => e.CreatedAt >= start && e.CreatedAt <= end && e.Status == EmailSendStatus.Sent)
            .GroupBy(e => e.CreatedAt.Date)
            .Select(g => new TimeSeriesPointDto(g.Key, g.Count(), null))
            .OrderBy(t => t.Date)
            .ToListAsync(ct);

        var result = new EmailAnalyticsDto(
            Math.Round(openRate, 2), Math.Round(clickRate, 2), Math.Round(replyRate, 2),
            Math.Round(bounceRate, 2), Math.Round(spamRate, 2), Math.Round(unsubRate, 2),
            new List<CampaignStatsDto>(), null, null, 0, 0, dailyStats);

        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    // ── Automation Analytics ─────────────────────────────────────────────────

    public async Task<AutomationAnalyticsDto> GetAutomationAnalyticsAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default)
    {
        var cacheKey = $"analytics:automation:{orgId}:{range.Preset}";
        if (_cache.TryGetValue(cacheKey, out AutomationAnalyticsDto? cached))
            return cached!;

        var (start, end) = range.Resolve();
        var execs = _db.AutomationExecutions.Where(e => e.CreatedAt >= start && e.CreatedAt <= end);

        var totalExecs = await execs.CountAsync(ct);
        var successful = await execs.CountAsync(e => e.Status == ExecutionStatus.Completed, ct);
        var failed = await execs.CountAsync(e => e.Status == ExecutionStatus.Failed, ct);
        var retried = await execs.CountAsync(e => e.RetryCount > 0, ct);
        var avgDuration = totalExecs > 0
            ? await execs.Where(e => e.DurationMs.HasValue).Select(e => (double)e.DurationMs!.Value).DefaultIfEmpty(0).AverageAsync(ct)
            : 0;

        // Top actions from logs
        var topActions = await _db.AutomationExecutionLogs
            .Where(l => l.Timestamp >= start && l.Timestamp <= end && l.ActionName != null)
            .GroupBy(l => l.ActionName!)
            .Select(g => new ActionUsageDto(g.Key, g.Count()))
            .OrderByDescending(a => a.Count)
            .Take(10)
            .ToListAsync(ct);

        // Top triggers
        var topTriggers = await execs
            .GroupBy(e => e.TriggerType)
            .Select(g => new TriggerUsageDto(g.Key.ToString(), g.Count()))
            .OrderByDescending(t => t.Count)
            .Take(10)
            .ToListAsync(ct);

        // Most active workflows
        var mostActive = await execs
            .GroupBy(e => new { e.WorkflowId, e.WorkflowName })
            .Select(g => new WorkflowStatsDto(g.Key.WorkflowId, g.Key.WorkflowName, g.Count(),
                g.Where(e => e.DurationMs.HasValue).Select(e => (double)e.DurationMs!.Value).DefaultIfEmpty(0).Average()))
            .OrderByDescending(w => w.ExecutionCount)
            .Take(10)
            .ToListAsync(ct);

        // Slowest workflows
        var slowest = await execs
            .Where(e => e.DurationMs.HasValue)
            .GroupBy(e => new { e.WorkflowId, e.WorkflowName })
            .Select(g => new WorkflowStatsDto(g.Key.WorkflowId, g.Key.WorkflowName, g.Count(),
                g.Select(e => (double)e.DurationMs!.Value).Average()))
            .OrderByDescending(w => w.AverageDurationMs)
            .Take(10)
            .ToListAsync(ct);

        var result = new AutomationAnalyticsDto(totalExecs, successful, failed, retried,
            Math.Round(avgDuration, 2), topActions, topTriggers, mostActive, slowest);

        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    // ── Airtable Analytics ───────────────────────────────────────────────────

    public async Task<AirtableAnalyticsDto> GetAirtableAnalyticsAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default)
    {
        var cacheKey = $"analytics:airtable:{orgId}:{range.Preset}";
        if (_cache.TryGetValue(cacheKey, out AirtableAnalyticsDto? cached))
            return cached!;

        var (start, end) = range.Resolve();
        var syncs = _db.AirtableSyncJobs.Where(s => s.CreatedAt >= start && s.CreatedAt <= end);

        var totalImports = await syncs.CountAsync(s => s.Direction == SyncDirection.AirtableToOreoLeads, ct);
        var totalExports = await syncs.CountAsync(s => s.Direction == SyncDirection.OreoLeadsToAirtable, ct);
        var conflicts = await syncs.SumAsync(s => s.ConflictRecords, ct);
        var retries = await syncs.CountAsync(s => s.AttemptCount > 1, ct);

        var completedSyncs = syncs.Where(s => s.CompletedAt.HasValue && s.StartedAt.HasValue);
        var avgDuration = await completedSyncs.AnyAsync(ct)
            ? await completedSyncs.Select(s => (double)(s.CompletedAt!.Value - s.StartedAt!.Value).TotalMilliseconds).AverageAsync(ct)
            : 0;

        var recentHistory = await syncs
            .OrderByDescending(s => s.CreatedAt)
            .Take(20)
            .Select(s => new SyncHistoryDto(
                s.CreatedAt,
                s.Direction.ToString(),
                s.Status.ToString(),
                s.CompletedAt.HasValue && s.StartedAt.HasValue
                    ? (long)(s.CompletedAt.Value - s.StartedAt.Value).TotalMilliseconds
                    : 0))
            .ToListAsync(ct);

        var result = new AirtableAnalyticsDto(totalImports, totalExports, conflicts, retries,
            Math.Round(avgDuration, 2), recentHistory);

        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    // ── Sales Funnel ─────────────────────────────────────────────────────────

    public async Task<FunnelDto> GetSalesFunnelAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default)
    {
        var cacheKey = $"analytics:funnel:{orgId}:{range.Preset}";
        if (_cache.TryGetValue(cacheKey, out FunnelDto? cached))
            return cached!;

        var (start, end) = range.Resolve();
        var leads = _db.Leads.Where(l => l.CreatedAt >= start && l.CreatedAt <= end);

        var statusOrder = new[]
        {
            LeadStatus.New,
            LeadStatus.Qualified,
            LeadStatus.ReadyToContact,
            LeadStatus.EmailSent,
            LeadStatus.FollowUp1,
            LeadStatus.Meeting,
            LeadStatus.ProposalSent,
            LeadStatus.Client
        };

        var stages = new List<FunnelStageDto>();
        var previousCount = 0;

        foreach (var status in statusOrder)
        {
            // Count leads that reached at least this stage
            var index = Array.IndexOf(statusOrder, status);
            var count = await leads.CountAsync(l => (int)l.Status >= (int)status, ct);

            var conversionRate = previousCount > 0 ? (double)count / previousCount * 100 : (index == 0 ? 100 : 0);
            var dropoffRate = previousCount > 0 ? (1 - (double)count / previousCount) * 100 : 0;

            stages.Add(new FunnelStageDto(status.ToString(), count, Math.Round(conversionRate, 2), 0, Math.Round(dropoffRate, 2)));
            previousCount = count > 0 ? count : previousCount;
        }

        var result = new FunnelDto(stages);
        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    // ── Time Series ──────────────────────────────────────────────────────────

    public async Task<List<TimeSeriesPointDto>> GetLeadTimeSeriesAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default)
    {
        var cacheKey = $"analytics:lead-series:{orgId}:{range.Preset}";
        if (_cache.TryGetValue(cacheKey, out List<TimeSeriesPointDto>? cached))
            return cached!;

        var (start, end) = range.Resolve();

        var result = await _db.Leads
            .Where(l => l.CreatedAt >= start && l.CreatedAt <= end)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new TimeSeriesPointDto(g.Key, g.Count(), null))
            .OrderBy(t => t.Date)
            .ToListAsync(ct);

        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<List<TimeSeriesPointDto>> GetEmailTimeSeriesAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default)
    {
        var cacheKey = $"analytics:email-series:{orgId}:{range.Preset}";
        if (_cache.TryGetValue(cacheKey, out List<TimeSeriesPointDto>? cached))
            return cached!;

        var (start, end) = range.Resolve();

        var result = await _db.EmailSendJobs
            .Where(e => e.CreatedAt >= start && e.CreatedAt <= end && e.Status == EmailSendStatus.Sent)
            .GroupBy(e => e.CreatedAt.Date)
            .Select(g => new TimeSeriesPointDto(g.Key, g.Count(), null))
            .OrderBy(t => t.Date)
            .ToListAsync(ct);

        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    // ── System Monitoring ────────────────────────────────────────────────────

    public async Task<MonitoringStatsDto> GetSystemMonitoringAsync(Guid? orgId, CancellationToken ct = default)
    {
        var avgWorkflowDuration = await _db.AutomationExecutions
            .Where(e => e.DurationMs.HasValue)
            .Select(e => (double)e.DurationMs!.Value)
            .DefaultIfEmpty(0)
            .AverageAsync(ct);

        var syncJobs = _db.AirtableSyncJobs.Where(s => s.CompletedAt.HasValue && s.StartedAt.HasValue);
        var avgSyncDuration = await syncJobs.AnyAsync(ct)
            ? await syncJobs.Select(s => (double)(s.CompletedAt!.Value - s.StartedAt!.Value).TotalMilliseconds).AverageAsync(ct)
            : 0;

        var queueDepth = await _db.AutomationQueueItems
            .CountAsync(q => q.Status == QueueItemStatus.Pending || q.Status == QueueItemStatus.Retrying, ct);
        var activeJobs = await _db.AutomationQueueItems
            .CountAsync(q => q.Status == QueueItemStatus.Running, ct);
        var failedJobs = await _db.AutomationQueueItems
            .CountAsync(q => q.Status == QueueItemStatus.Failed, ct);

        return new MonitoringStatsDto(
            0, Math.Round(avgWorkflowDuration, 2), Math.Round(avgSyncDuration, 2), 0,
            5, queueDepth, activeJobs, failedJobs, DateTime.UtcNow);
    }

    // ── Cache Invalidation ───────────────────────────────────────────────────

    public void InvalidateCacheForOrg(Guid? orgId)
    {
        var prefixes = new[]
        {
            "analytics:dashboard", "analytics:kpi", "analytics:email",
            "analytics:automation", "analytics:airtable", "analytics:funnel",
            "analytics:lead-series", "analytics:email-series"
        };

        foreach (var preset in Enum.GetValues<DateRangePreset>())
        {
            foreach (var prefix in prefixes)
            {
                _cache.Remove($"{prefix}:{orgId}:{preset}");
            }
        }
    }
}
