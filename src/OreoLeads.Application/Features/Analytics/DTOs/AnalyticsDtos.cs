using OreoLeads.Domain.Enums;

namespace OreoLeads.Application.Features.Analytics.DTOs;

// ── Input ────────────────────────────────────────────────────────────────────

public record DateRangeDto(DateRangePreset Preset, DateTime? From, DateTime? To)
{
    public (DateTime Start, DateTime End) Resolve()
    {
        var now = DateTime.UtcNow;
        return Preset switch
        {
            DateRangePreset.Today      => (now.Date, now),
            DateRangePreset.Yesterday  => (now.Date.AddDays(-1), now.Date.AddTicks(-1)),
            DateRangePreset.Last7Days  => (now.Date.AddDays(-7), now),
            DateRangePreset.Last30Days => (now.Date.AddDays(-30), now),
            DateRangePreset.Last90Days => (now.Date.AddDays(-90), now),
            DateRangePreset.ThisYear   => (new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), now),
            DateRangePreset.Custom     => (From ?? now.AddDays(-30), To ?? now),
            _                          => (now.Date.AddDays(-30), now)
        };
    }
}

// ── Executive Dashboard ──────────────────────────────────────────────────────

public record ExecutiveDashboardDto(
    LeadStatsDto Leads,
    EmailStatsDto Emails,
    AutomationStatsDto Automation,
    AirtableStatsDto Airtable,
    int PendingFollowUps,
    List<TopUserActivityDto> UserActivity,
    DateTime GeneratedAt);

public record LeadStatsDto(
    int Today, int ThisWeek, int ThisMonth, int ThisYear,
    int NewProspects, int Converted, double ConversionRate);

public record EmailStatsDto(
    int Sent, int Delivered, int Opened, int Clicked,
    int Replied, int Bounced, int Unsubscribed,
    double OpenRate, double ClickRate, double ReplyRate, double BounceRate);

public record AutomationStatsDto(
    int TotalExecutions, int Successful, int Failed, int Retried,
    double SuccessRate, double AverageDurationMs);

public record AirtableStatsDto(
    int TotalSyncs, int Successful, int Failed, int Conflicts,
    double SuccessRate);

public record TopUserActivityDto(string UserId, string? UserName, int ActionCount);

// ── KPI ──────────────────────────────────────────────────────────────────────

public record KpiSummaryDto(
    double ConversionRate, double ReplyRate, double OpenRate,
    double ClickRate, double BounceRate, double LeadVelocity,
    double AverageResponseTimeHours, double AverageConversionTimeDays,
    double AutomationSuccessRate, double AutomationFailureRate, double AutomationRetryRate,
    double AirtableSyncSuccess,
    double EmailsPerDay, double LeadsPerDay, double WorkflowsPerDay);

// ── Email Analytics ──────────────────────────────────────────────────────────

public record EmailAnalyticsDto(
    double OpenRate, double ClickRate, double ReplyRate,
    double BounceRate, double SpamRate, double UnsubscribeRate,
    List<CampaignStatsDto> TopCampaigns,
    double? BestHourOfDay, string? BestDayOfWeek,
    double AverageMinutesToOpen, double AverageMinutesToReply,
    List<TimeSeriesPointDto> DailyStats);

public record CampaignStatsDto(string Name, int Sent, int Opened, int Clicked, double OpenRate);

// ── Automation Analytics ─────────────────────────────────────────────────────

public record AutomationAnalyticsDto(
    int TotalExecutions, int Successful, int Failed, int Retried,
    double AverageDurationMs,
    List<ActionUsageDto> TopActions,
    List<TriggerUsageDto> TopTriggers,
    List<WorkflowStatsDto> MostActiveWorkflows,
    List<WorkflowStatsDto> SlowestWorkflows);

public record ActionUsageDto(string ActionType, int Count);
public record TriggerUsageDto(string TriggerType, int Count);
public record WorkflowStatsDto(Guid Id, string Name, int ExecutionCount, double AverageDurationMs);

// ── Airtable Analytics ───────────────────────────────────────────────────────

public record AirtableAnalyticsDto(
    int TotalImports, int TotalExports, int Conflicts, int Retries,
    double AverageDurationMs,
    List<SyncHistoryDto> RecentHistory);

public record SyncHistoryDto(DateTime SyncedAt, string Direction, string Status, long DurationMs);

// ── Funnel ───────────────────────────────────────────────────────────────────

public record FunnelDto(List<FunnelStageDto> Stages);
public record FunnelStageDto(string Name, int Count, double ConversionRate, double AverageDaysInStage, double DropoffRate);

// ── Time Series ──────────────────────────────────────────────────────────────

public record TimeSeriesPointDto(DateTime Date, double Value, string? Label);

// ── Monitoring ───────────────────────────────────────────────────────────────

public record MonitoringStatsDto(
    double AverageApiResponseMs,
    double AverageWorkflowDurationMs,
    double AverageSyncDurationMs,
    double AverageEmailSendMs,
    int ActiveBackgroundServices,
    int QueueDepth,
    int ActiveJobs,
    int FailedJobs,
    DateTime GeneratedAt);

// ── Forecast ─────────────────────────────────────────────────────────────────

public record ForecastPointDto(DateTime Date, double Value, double ConfidenceLow, double ConfidenceHigh);
public record ForecastSummaryDto(
    List<ForecastPointDto> LeadsForecast,
    List<ForecastPointDto> ConversionsForecast,
    List<ForecastPointDto> EmailsForecast,
    double ProjectedLeadsNextMonth,
    double ProjectedConversionsNextMonth);

// ── Widget DTOs ──────────────────────────────────────────────────────────────

public record SaveDashboardDto(string Name, string? Description, bool IsDefault);
public record AddWidgetDto(Guid DashboardId, string Title, WidgetType Type, string? ConfigJson, int SortOrder);
public record UpdateWidgetDto(string Title, string? ConfigJson, string? PositionJson, bool IsVisible);

// ── Report DTOs ──────────────────────────────────────────────────────────────

public record CreateReportDto(string Name, string? Description, string ReportType, string? FilterJson, ReportFormat Format);
public record ExportRequestDto(string ReportType, DateRangePreset Preset, DateTime? From, DateTime? To, ReportFormat Format);
public record SaveScheduledReportDto(string Name, string ReportType, ReportFrequency Frequency, string Recipients, ReportFormat Format, string? FilterJson, bool IsEnabled);
