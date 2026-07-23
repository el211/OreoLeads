namespace OreoLeads.Infrastructure.Enrichment;

public sealed class BraveSearchSettings
{
    public const string Section = "BraveSearch";

    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.search.brave.com/res/v1/web/search";
    public int ResultsPerQuery { get; set; } = 10;
    /// <summary>Le palier gratuit Brave est limité à ~1 requête/seconde.</summary>
    public int MinIntervalMs { get; set; } = 1100;
    /// <summary>Durée de cache d'une requête identique (évite de consommer le quota).</summary>
    public int CacheHours { get; set; } = 168;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
