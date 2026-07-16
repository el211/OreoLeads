using OreoLeads.Application.Features.Dashboard.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default);
}
