using OreoLeads.Application.Features.Analytics.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface IForecastService
{
    Task<List<ForecastPointDto>> ForecastLeadsAsync(Guid? orgId, int daysAhead, CancellationToken ct = default);
    Task<List<ForecastPointDto>> ForecastConversionsAsync(Guid? orgId, int daysAhead, CancellationToken ct = default);
    Task<List<ForecastPointDto>> ForecastEmailsAsync(Guid? orgId, int daysAhead, CancellationToken ct = default);
    Task<ForecastSummaryDto> GetForecastSummaryAsync(Guid? orgId, CancellationToken ct = default);
}
