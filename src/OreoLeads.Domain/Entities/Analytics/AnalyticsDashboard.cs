using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities.Analytics;

public class AnalyticsDashboard : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? UserId { get; set; }
    public bool IsDefault { get; set; }
    public string? LayoutJson { get; set; }
}
