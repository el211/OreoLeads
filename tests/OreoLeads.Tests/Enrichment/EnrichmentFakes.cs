using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Tests.Enrichment;

/// <summary>Client Brave factice : renvoie des résultats prédéfinis par requête (ou une liste par défaut).</summary>
internal sealed class FakeBraveClient : IBraveSearchClient
{
    private readonly List<WebSearchResult> _defaultResults;
    public int CallCount { get; private set; }
    public bool IsConfigured { get; init; } = true;

    public FakeBraveClient(params WebSearchResult[] results)
        => _defaultResults = results.ToList();

    public Task<List<WebSearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        CallCount++;
        return Task.FromResult(_defaultResults);
    }
}

/// <summary>Fetcher factice : renvoie du HTML par URL (préfixe), sinon vide.</summary>
internal sealed class FakePageFetcher : IPageFetcher
{
    private readonly Dictionary<string, string> _pages;

    public FakePageFetcher(Dictionary<string, string>? pages = null)
        => _pages = pages ?? new(StringComparer.OrdinalIgnoreCase);

    public FakePageFetcher Add(string url, string html)
    {
        _pages[url] = html;
        return this;
    }

    public Task<PageFetchResult> FetchAsync(string url, CancellationToken ct = default)
    {
        var html = _pages.TryGetValue(url, out var h) ? h : "";
        return Task.FromResult(new PageFetchResult(
            Html: html, FinalUrl: url, StatusCode: html.Length > 0 ? 200 : 404,
            ResponseTimeMs: 1, RedirectCount: 0, CertificateValid: true, UsedBrowser: false, Error: null));
    }
}
