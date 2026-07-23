namespace OreoLeads.Application.Common.Interfaces;

public sealed record PageFetchResult(
    string? Html,
    string FinalUrl,
    int StatusCode,
    int ResponseTimeMs,
    int RedirectCount,
    bool CertificateValid,
    bool UsedBrowser,
    string? Error);

/// <summary>
/// Récupère le HTML d'une page publique. Implémentations : HttpClient (rapide)
/// et Playwright (rendu JavaScript). L'appelant doit valider l'URL (SSRF) avant.
/// </summary>
public interface IPageFetcher
{
    Task<PageFetchResult> FetchAsync(string url, CancellationToken ct = default);
}
