using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Entities.Analytics;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Analytics;

internal sealed class WidgetService : IWidgetService
{
    private readonly ApplicationDbContext _db;

    public WidgetService(ApplicationDbContext db) => _db = db;

    public async Task<List<AnalyticsDashboard>> GetDashboardsAsync(Guid? orgId, string? userId, CancellationToken ct = default)
    {
        return await _db.AnalyticsDashboards
            .Where(d => d.OrganizationId == orgId)
            .OrderByDescending(d => d.IsDefault)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<AnalyticsDashboard> GetOrCreateDefaultDashboardAsync(Guid? orgId, string? userId, CancellationToken ct = default)
    {
        var existing = await _db.AnalyticsDashboards
            .FirstOrDefaultAsync(d => d.OrganizationId == orgId && d.IsDefault, ct);

        if (existing is not null)
            return existing;

        var dashboard = new AnalyticsDashboard
        {
            Name = "Dashboard principal",
            Description = "Dashboard executif par defaut",
            OrganizationId = orgId,
            UserId = userId,
            IsDefault = true
        };
        _db.AnalyticsDashboards.Add(dashboard);

        // Default widgets
        var defaultWidgets = new List<AnalyticsWidget>
        {
            new() { DashboardId = dashboard.Id, Title = "Leads aujourd'hui", Type = WidgetType.KpiCard, SortOrder = 0, OrganizationId = orgId },
            new() { DashboardId = dashboard.Id, Title = "Emails envoyes", Type = WidgetType.KpiCard, SortOrder = 1, OrganizationId = orgId },
            new() { DashboardId = dashboard.Id, Title = "Taux de conversion", Type = WidgetType.KpiCard, SortOrder = 2, OrganizationId = orgId },
            new() { DashboardId = dashboard.Id, Title = "Automations actives", Type = WidgetType.KpiCard, SortOrder = 3, OrganizationId = orgId },
            new() { DashboardId = dashboard.Id, Title = "Evolution leads", Type = WidgetType.LineChart, SortOrder = 4, OrganizationId = orgId },
            new() { DashboardId = dashboard.Id, Title = "Emails par jour", Type = WidgetType.BarChart, SortOrder = 5, OrganizationId = orgId },
        };

        _db.AnalyticsWidgets.AddRange(defaultWidgets);
        await _db.SaveChangesAsync(ct);
        return dashboard;
    }

    public async Task<AnalyticsDashboard> SaveDashboardAsync(SaveDashboardDto dto, Guid? orgId, string? userId, CancellationToken ct = default)
    {
        var dashboard = new AnalyticsDashboard
        {
            Name = dto.Name,
            Description = dto.Description,
            IsDefault = dto.IsDefault,
            OrganizationId = orgId,
            UserId = userId
        };

        _db.AnalyticsDashboards.Add(dashboard);
        await _db.SaveChangesAsync(ct);
        return dashboard;
    }

    public async Task<List<AnalyticsWidget>> GetWidgetsAsync(Guid dashboardId, CancellationToken ct = default)
    {
        return await _db.AnalyticsWidgets
            .Where(w => w.DashboardId == dashboardId)
            .OrderBy(w => w.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<AnalyticsWidget> AddWidgetAsync(AddWidgetDto dto, Guid? orgId, CancellationToken ct = default)
    {
        var widget = new AnalyticsWidget
        {
            DashboardId = dto.DashboardId,
            Title = dto.Title,
            Type = dto.Type,
            ConfigJson = dto.ConfigJson,
            SortOrder = dto.SortOrder,
            OrganizationId = orgId
        };

        _db.AnalyticsWidgets.Add(widget);
        await _db.SaveChangesAsync(ct);
        return widget;
    }

    public async Task<AnalyticsWidget> UpdateWidgetAsync(Guid widgetId, UpdateWidgetDto dto, CancellationToken ct = default)
    {
        var widget = await _db.AnalyticsWidgets.FindAsync([widgetId], ct)
            ?? throw new InvalidOperationException($"Widget {widgetId} not found");

        widget.Title = dto.Title;
        widget.ConfigJson = dto.ConfigJson;
        widget.PositionJson = dto.PositionJson;
        widget.IsVisible = dto.IsVisible;
        widget.SetUpdatedAt();

        await _db.SaveChangesAsync(ct);
        return widget;
    }

    public async Task DeleteWidgetAsync(Guid widgetId, CancellationToken ct = default)
    {
        var widget = await _db.AnalyticsWidgets.FindAsync([widgetId], ct);
        if (widget is null) return;

        _db.AnalyticsWidgets.Remove(widget);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveLayoutAsync(Guid dashboardId, string layoutJson, CancellationToken ct = default)
    {
        var dashboard = await _db.AnalyticsDashboards.FindAsync([dashboardId], ct)
            ?? throw new InvalidOperationException($"Dashboard {dashboardId} not found");

        dashboard.LayoutJson = layoutJson;
        dashboard.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }
}
