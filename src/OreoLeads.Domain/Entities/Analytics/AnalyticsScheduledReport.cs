using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities.Analytics;

public class AnalyticsScheduledReport : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public ReportFrequency Frequency { get; set; }
    public string Recipients { get; set; } = string.Empty;
    public string? FilterJson { get; set; }
    public ReportFormat Format { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastSentAt { get; set; }
    public DateTime? NextSendAt { get; set; }
    public Guid? OrganizationId { get; set; }
}
