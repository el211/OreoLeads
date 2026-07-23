using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Infrastructure.Enrichment;

/// <summary>
/// Client Brave Search API. Singleton : limiteur de débit partagé (palier gratuit
/// ≈ 1 req/s, 2 000 req/mois) + cache mémoire des requêtes identiques + gestion 429.
/// </summary>
public sealed class BraveSearchClient : IBraveSearchClient
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly BraveSearchSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BraveSearchClient> _logger;

    private readonly SemaphoreSlim _rateLock = new(1, 1);
    private DateTime _lastCallUtc = DateTime.MinValue;
    private long _totalQueries;

    public BraveSearchClient(
        IHttpClientFactory httpFactory,
        IOptions<BraveSearchSettings> settings,
        IMemoryCache cache,
        ILogger<BraveSearchClient> logger)
    {
        _httpFactory = httpFactory;
        _settings    = settings.Value;
        _cache       = cache;
        _logger      = logger;
    }

    public bool IsConfigured => _settings.IsConfigured;

    public async Task<List<WebSearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Brave Search API key is not configured (BraveSearch:ApiKey).");

        var cacheKey = $"brave:{query.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out List<WebSearchResult>? cached) && cached is not null)
        {
            _logger.LogDebug("Brave cache hit for query: {Query}", query);
            return cached;
        }

        var results = await ExecuteAsync(query, ct);
        _cache.Set(cacheKey, results, TimeSpan.FromHours(_settings.CacheHours));
        return results;
    }

    private async Task<List<WebSearchResult>> ExecuteAsync(string query, CancellationToken ct)
    {
        await _rateLock.WaitAsync(ct);
        try
        {
            // Limiteur de débit : espace les appels de MinIntervalMs
            var sinceLast = DateTime.UtcNow - _lastCallUtc;
            var minInterval = TimeSpan.FromMilliseconds(_settings.MinIntervalMs);
            if (sinceLast < minInterval)
                await Task.Delay(minInterval - sinceLast, ct);

            var response = await SendAsync(query, ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                _logger.LogWarning("Brave 429 — retry after {Delay}s", retryAfter.TotalSeconds);
                response.Dispose();
                await Task.Delay(retryAfter, ct);
                response = await SendAsync(query, ct);
            }

            using (response)
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync(ct);
                var parsed = JsonSerializer.Deserialize<BraveResponse>(body);

                var count = Interlocked.Increment(ref _totalQueries);
                _logger.LogInformation("Brave query #{Count}: \"{Query}\" → {Results} résultats",
                    count, query, parsed?.Web?.Results?.Count ?? 0);

                return parsed?.Web?.Results?
                    .Where(r => !string.IsNullOrWhiteSpace(r.Url))
                    .Take(_settings.ResultsPerQuery)
                    .Select(r => new WebSearchResult(r.Url!, r.Title ?? "", r.Description ?? ""))
                    .ToList() ?? [];
            }
        }
        finally
        {
            _lastCallUtc = DateTime.UtcNow;
            _rateLock.Release();
        }
    }

    private async Task<HttpResponseMessage> SendAsync(string query, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient(nameof(BraveSearchClient));
        var url = $"{_settings.BaseUrl}?q={Uri.EscapeDataString(query)}&count={_settings.ResultsPerQuery}&country=fr&search_lang=fr";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Subscription-Token", _settings.ApiKey);
        request.Headers.Add("Accept", "application/json");
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    // ── Modèles de désérialisation ──────────────────────────────────────────

    private sealed class BraveResponse
    {
        [JsonPropertyName("web")]
        public BraveWeb? Web { get; set; }
    }

    private sealed class BraveWeb
    {
        [JsonPropertyName("results")]
        public List<BraveResult>? Results { get; set; }
    }

    private sealed class BraveResult
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
