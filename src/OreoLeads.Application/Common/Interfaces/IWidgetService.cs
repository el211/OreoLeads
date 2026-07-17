using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Entities.Analytics;

namespace OreoLeads.Application.Common.Interfaces;

public interface IWidgetService
{
    Task<List<AnalyticsDashboard>> GetDashboardsAsync(Guid? orgId, string? userId, CancellationToken ct = default);
    Task<AnalyticsDashboard> GetOrCreateDefaultDashboardAsync(Guid? orgId, string? userId, CancellationToken ct = default);
    Task<AnalyticsDashboard> SaveDashboardAsync(SaveDashboardDto dto, Guid? orgId, string? userId, CancellationToken ct = default);
    Task<List<AnalyticsWidget>> GetWidgetsAsync(Guid dashboardId, CancellationToken ct = default);
    Task<AnalyticsWidget> AddWidgetAsync(AddWidgetDto dto, Guid? orgId, CancellationToken ct = default);
    Task<AnalyticsWidget> UpdateWidgetAsync(Guid widgetId, UpdateWidgetDto dto, CancellationToken ct = default);
    Task DeleteWidgetAsync(Guid widgetId, CancellationToken ct = default);
    Task SaveLayoutAsync(Guid dashboardId, string layoutJson, CancellationToken ct = default);
}
