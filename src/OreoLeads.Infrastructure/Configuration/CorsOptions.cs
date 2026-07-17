namespace OreoLeads.Infrastructure.Configuration;

public class CorsOptions
{
    public const string SectionName = "Cors";
    public string[] AllowedOrigins { get; set; } = [];
    public bool AllowCredentials { get; set; } = true;
}
