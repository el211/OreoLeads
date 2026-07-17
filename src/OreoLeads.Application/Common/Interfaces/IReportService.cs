using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Entities.Analytics;

namespace OreoLeads.Application.Common.Interfaces;

public interface IReportService
{
    Task<AnalyticsReport> CreateReportAsync(CreateReportDto dto, Guid? orgId, CancellationToken ct = default);
    Task<List<AnalyticsReport>> GetReportsAsync(Guid? orgId, CancellationToken ct = default);
    Task<byte[]> ExportAsync(ExportRequestDto dto, Guid? orgId, CancellationToken ct = default);
    Task<List<AnalyticsScheduledReport>> GetScheduledReportsAsync(Guid? orgId, CancellationToken ct = default);
    Task<AnalyticsScheduledReport> SaveScheduledReportAsync(SaveScheduledReportDto dto, Guid? orgId, CancellationToken ct = default);
    Task DeleteScheduledReportAsync(Guid id, CancellationToken ct = default);
}
