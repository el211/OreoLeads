using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Infrastructure.Fetching;

/// <summary>
/// Sélectionne le fetcher : Playwright (rendu JS) quand il est disponible, sinon
/// HttpClient. Bascule aussi sur HTTP par requête si le rendu navigateur échoue.
/// </summary>
public sealed class CompositePageFetcher : IPageFetcher
{
    private readonly PlaywrightPageFetcher _playwright;
    private readonly HttpPageFetcher _http;
    private readonly ILogger<CompositePageFetcher> _logger;

    public CompositePageFetcher(
        PlaywrightPageFetcher playwright,
        HttpPageFetcher http,
        ILogger<CompositePageFetcher> logger)
    {
        _playwright = playwright;
        _http       = http;
        _logger     = logger;
    }

    public async Task<PageFetchResult> FetchAsync(string url, CancellationToken ct = default)
    {
        if (_playwright.IsAvailable)
        {
            var result = await _playwright.FetchAsync(url, ct);
            if (result.Error is null && !string.IsNullOrEmpty(result.Html))
                return result;

            _logger.LogDebug("Playwright n'a rien renvoyé pour {Url}, bascule HTTP", url);
        }

        return await _http.FetchAsync(url, ct);
    }
}
