using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities.Analytics;

public class AnalyticsReport : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string? FilterJson { get; set; }
    public Guid? OrganizationId { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public string? FilePath { get; set; }
    public ReportFormat Format { get; set; }
    public string? ErrorMessage { get; set; }
}
