using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Infrastructure.Enrichment;

/// <summary>
/// Découvre le site officiel probable d'une entreprise via Brave Search.
/// Ne choisit JAMAIS le premier résultat automatiquement : chaque candidat est
/// visité et scoré (nom, ville, téléphone, SIREN, domaine). Les annuaires,
/// réseaux sociaux et plateformes de réservation ne sont jamais retenus comme
/// site officiel, mais conservés comme profils externes.
/// </summary>
public sealed class WebsiteDiscoveryService : IWebsiteDiscoveryService
{
    private const int MaxCandidatesToScore = 5;

    private readonly IBraveSearchClient _search;
    private readonly IPageFetcher _fetcher;
    private readonly EnrichmentSettings _settings;
    private readonly ILogger<WebsiteDiscoveryService> _logger;

    public WebsiteDiscoveryService(
        IBraveSearchClient search,
        IPageFetcher fetcher,
        IOptions<EnrichmentSettings> settings,
        ILogger<WebsiteDiscoveryService> logger)
    {
        _search   = search;
        _fetcher  = fetcher;
        _settings = settings.Value;
        _logger   = logger;
    }

    public bool IsConfigured => _search.IsConfigured;

    public async Task<WebsiteDiscoveryResult> DiscoverAsync(Lead lead, CancellationToken ct = default)
    {
        var identity = new EnrichmentScoring.CompanyIdentity(
            DisplayName: !string.IsNullOrWhiteSpace(lead.TradeName) ? lead.TradeName : lead.CompanyName,
            City:        lead.City,
            PostalCode:  lead.PostalCode,
            Phone:       lead.Phone,
            Siren:       lead.Siren,
            Siret:       lead.Siret);

        var externalProfiles = new List<ExternalProfile>();
        var scored = new Dictionary<string, WebsiteCandidate>(StringComparer.OrdinalIgnoreCase);
        var queriesUsed = 0;

        foreach (var query in BuildQueries(lead, identity.DisplayName))
        {
            if (queriesUsed >= _settings.MaxQueriesPerLead) break;
            ct.ThrowIfCancellationRequested();

            List<WebSearchResult> results;
            try
            {
                results = await _search.SearchAsync(query, ct);
                queriesUsed++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Recherche Brave échouée pour \"{Query}\"", query);
                continue;
            }

            foreach (var result in results)
            {
                if (EnrichmentScoring.IsBlacklisted(result.Url, _settings.DirectoryBlacklist))
                {
                    var category = CategorizeExternal(result.Url);
                    if (category is not null &&
                        !externalProfiles.Any(p => p.Url.Equals(result.Url, StringComparison.OrdinalIgnoreCase)))
                        externalProfiles.Add(new ExternalProfile(result.Url, category));
                    continue;
                }

                var domain = EnrichmentScoring.GetRegistrableDomain(result.Url);
                if (domain is null || scored.ContainsKey(domain)) continue;
                if (scored.Count >= MaxCandidatesToScore) continue;

                var candidate = await ScoreCandidateAsync(identity, result.Url, ct);
                if (candidate is not null)
                    scored[domain] = candidate;
            }

            // Arrêt anticipé si un candidat dépasse déjà le seuil d'application
            if (scored.Values.Any(c => c.Score >= _settings.AutoApplyThreshold)) break;
        }

        var ordered = scored.Values.OrderByDescending(c => c.Score).ToList();
        var best = ordered.FirstOrDefault();

        return new WebsiteDiscoveryResult(
            ChosenUrl:       best?.Score >= _settings.ReviewThreshold ? best.Url : null,
            Confidence:      best?.Score ?? 0,
            MatchedSignals:  best?.Signals ?? [],
            Candidates:      ordered,
            ExternalProfiles: externalProfiles,
            QueriesUsed:     queriesUsed);
    }

    private static IEnumerable<string> BuildQueries(Lead lead, string displayName)
    {
        if (!string.IsNullOrWhiteSpace(lead.City))
            yield return $"\"{displayName}\" {lead.City}";

        if (!string.IsNullOrWhiteSpace(lead.CompanyName) &&
            !lead.CompanyName.Equals(displayName, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(lead.PostalCode))
            yield return $"\"{lead.CompanyName}\" {lead.PostalCode}";

        yield return $"\"{displayName}\" contact";

        if (!string.IsNullOrWhiteSpace(lead.Siren))
            yield return lead.Siren!;

        if (!string.IsNullOrWhiteSpace(lead.Phone))
            yield return $"\"{lead.Phone}\"";

        var entrepreneur = $"{lead.EntrepreneurFirstName} {lead.EntrepreneurLastName}".Trim();
        if (entrepreneur.Length > 3 && !string.IsNullOrWhiteSpace(lead.City))
            yield return $"{entrepreneur} {lead.Industry} {lead.City}".Trim();
    }

    private async Task<WebsiteCandidate?> ScoreCandidateAsync(
        EnrichmentScoring.CompanyIdentity identity, string url, CancellationToken ct)
    {
        try
        {
            await SsrfGuard.ValidateAsync(url, ct);
            var page = await _fetcher.FetchAsync(url, ct);
            if (string.IsNullOrEmpty(page.Html)) return null;

            var result = EnrichmentScoring.ScoreCandidate(identity, url, page.Html);
            return new WebsiteCandidate(url, result.Score, "OfficialCandidate", result.Signals);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Candidat {Url} ignoré", url);
            return null;
        }
    }

    private static string? CategorizeExternal(string url)
    {
        var domain = EnrichmentScoring.GetRegistrableDomain(url);
        if (domain is null) return null;

        if (EnrichmentSettings.SocialDomains.Any(d => domain.EndsWith(d, StringComparison.OrdinalIgnoreCase)))
            return "Social";
        if (EnrichmentSettings.BookingDomains.Any(d => domain.EndsWith(d, StringComparison.OrdinalIgnoreCase)))
            return "Booking";
        if (EnrichmentSettings.LegalRegistryDomains.Any(d => domain.EndsWith(d, StringComparison.OrdinalIgnoreCase)))
            return "RegistreLegal";
        return "Annuaire";
    }
}
