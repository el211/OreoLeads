using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities.Analytics;

public class AnalyticsWidget : BaseEntity
{
    public Guid DashboardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public WidgetType Type { get; set; }
    public string? ConfigJson { get; set; }
    public string? PositionJson { get; set; }
    public Guid? OrganizationId { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    // Navigation
    public AnalyticsDashboard? Dashboard { get; set; }
}
