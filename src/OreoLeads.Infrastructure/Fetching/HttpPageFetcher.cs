using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Infrastructure.Fetching;

/// <summary>
/// Récupération HTTP simple (sans rendu JavaScript). Réessaie sans validation SSL
/// quand le certificat est invalide, pour pouvoir quand même analyser le HTML.
/// </summary>
public sealed class HttpPageFetcher : IPageFetcher
{
    private readonly ILogger<HttpPageFetcher> _logger;

    public HttpPageFetcher(ILogger<HttpPageFetcher> logger) => _logger = logger;

    public async Task<PageFetchResult> FetchAsync(string url, CancellationToken ct = default)
    {
        var result = await TryFetchAsync(url, validateSsl: true, ct);
        if (result.Error != null && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("SSL invalide pour {Url}, réessai sans validation", url);
            var retry = await TryFetchAsync(url, validateSsl: false, ct);
            return retry with { CertificateValid = false };
        }
        return result;
    }

    private async Task<PageFetchResult> TryFetchAsync(string url, bool validateSsl, CancellationToken ct)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            ServerCertificateCustomValidationCallback =
                validateSsl ? null : (_, _, _, _) => true,
        };

        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15),
        };
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (compatible; OreoLeads/1.0; +https://oreostudios.fr)");
        client.DefaultRequestHeaders.Add("Accept-Language", "fr-FR,fr;q=0.9");

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead, ct);
            sw.Stop();

            var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
            var redirected = !string.Equals(url, finalUrl, StringComparison.OrdinalIgnoreCase);

            var html = string.Empty;
            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
                html = await response.Content.ReadAsStringAsync(ct);

            return new PageFetchResult(
                Html: html,
                FinalUrl: finalUrl,
                StatusCode: (int)response.StatusCode,
                ResponseTimeMs: (int)sw.ElapsedMilliseconds,
                RedirectCount: redirected ? 1 : 0,
                CertificateValid: url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && validateSsl,
                UsedBrowser: false,
                Error: null);
        }
        catch (HttpRequestException ex) when (
            ex.InnerException is System.Security.Authentication.AuthenticationException)
        {
            return Failed(url, sw, $"Certificat SSL invalide : {ex.InnerException.Message}");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Failed(url, sw, "Timeout (>15 s)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erreur de fetch pour {Url}", url);
            return Failed(url, sw, ex.Message);
        }
    }

    private static PageFetchResult Failed(string url, Stopwatch sw, string error)
        => new(null, url, 0, (int)sw.ElapsedMilliseconds, 0, false, false, error);
}
