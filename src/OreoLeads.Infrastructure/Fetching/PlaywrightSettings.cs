namespace OreoLeads.Infrastructure.Fetching;

public sealed class PlaywrightSettings
{
    public const string Section = "Playwright";

    /// <summary>Active le rendu navigateur. Désactivé par défaut en dev (pas de Chromium installé).</summary>
    public bool Enabled { get; set; }
    public int NavigationTimeoutSeconds { get; set; } = 20;
    public int PostLoadDelayMs { get; set; } = 750;
    public int MaxConcurrentPages { get; set; } = 3;
}
