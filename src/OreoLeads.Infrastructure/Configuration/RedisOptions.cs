namespace OreoLeads.Infrastructure.Configuration;

public class RedisOptions
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; set; } = string.Empty;
    public int DefaultExpiryMinutes { get; set; } = 60;
    public bool Enabled { get; set; } = true;
}
