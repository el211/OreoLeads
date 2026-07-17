namespace OreoLeads.Infrastructure.Configuration;

public class ObservabilityOptions
{
    public const string SectionName = "Observability";
    public string ServiceName { get; set; } = "OreoLeads";
    public string ServiceVersion { get; set; } = "1.0.0";
    public bool EnableTracing { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public string? OtlpEndpoint { get; set; }
}
