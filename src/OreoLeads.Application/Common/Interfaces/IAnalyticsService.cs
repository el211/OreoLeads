using OreoLeads.Application.Features.Analytics.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface IAnalyticsService
{
    Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default);
    Task<KpiSummaryDto> GetKpiSummaryAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default);
    Task<EmailAnalyticsDto> GetEmailAnalyticsAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default);
    Task<AutomationAnalyticsDto> GetAutomationAnalyticsAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default);
    Task<AirtableAnalyticsDto> GetAirtableAnalyticsAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default);
    Task<FunnelDto> GetSalesFunnelAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default);
    Task<List<TimeSeriesPointDto>> GetLeadTimeSeriesAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default);
    Task<List<TimeSeriesPointDto>> GetEmailTimeSeriesAsync(Guid? orgId, DateRangeDto range, CancellationToken ct = default);
    Task<MonitoringStatsDto> GetSystemMonitoringAsync(Guid? orgId, CancellationToken ct = default);
    void InvalidateCacheForOrg(Guid? orgId);
}
