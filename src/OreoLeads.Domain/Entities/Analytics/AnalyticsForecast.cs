using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities.Analytics;

public class AnalyticsForecast : BaseEntity
{
    public string MetricName { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public double Value { get; set; }
    public double ConfidenceLow { get; set; }
    public double ConfidenceHigh { get; set; }
    public ForecastMethod Method { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public Guid? OrganizationId { get; set; }
}
