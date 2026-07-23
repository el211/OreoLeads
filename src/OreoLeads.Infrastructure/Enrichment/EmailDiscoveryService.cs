using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Analysis;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Infrastructure.Enrichment;

/// <summary>
/// Recherche d'e-mails publics : page d'accueil, pages contact, mentions légales,
/// à-propos, puis chemins fixes usuels. Ne devine jamais d'adresse.
/// </summary>
public sealed class EmailDiscoveryService : IEmailDiscoveryService
{
    private const int MaxPagesToScan = 5;

    private static readonly string[] FixedPaths =
        ["/contact", "/contactez-nous", "/mentions-legales", "/mentions", "/a-propos", "/about", "/equipe", "/reservation"];

    private readonly IPageFetcher _fetcher;
    private readonly ILogger<EmailDiscoveryService> _logger;

    public EmailDiscoveryService(IPageFetcher fetcher, ILogger<EmailDiscoveryService> logger)
    {
        _fetcher = fetcher;
        _logger  = logger;
    }

    public async Task<EmailDiscoveryResult> DiscoverFromWebsiteAsync(
        string websiteUrl, CancellationToken ct = default)
    {
        var pagesToScan = new List<string> { websiteUrl };
        var scanned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allFound = new List<(string Email, string SourceUrl)>();

        var homepage = await FetchSafeAsync(websiteUrl, ct);
        if (homepage is not null)
        {
            scanned.Add(websiteUrl);
            CollectEmails(homepage, websiteUrl, allFound);

            if (Uri.TryCreate(websiteUrl, UriKind.Absolute, out var baseUri))
            {
                pagesToScan.AddRange(HtmlAnalyzer.FindContactPageUrls(homepage, baseUri));
                pagesToScan.AddRange(HtmlAnalyzer.FindLegalPageUrls(homepage, baseUri));
                pagesToScan.AddRange(FixedPaths.Select(p => new Uri(baseUri, p).ToString()));
            }
        }

        foreach (var url in pagesToScan.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (scanned.Count >= MaxPagesToScan) break;
            if (!scanned.Add(url)) continue;

            var html = await FetchSafeAsync(url, ct);
            if (html is not null)
                CollectEmails(html, url, allFound);

            // Un e-mail du même domaine suffit — inutile de scanner davantage
            if (allFound.Any(f => EmailExtractor.IsSameDomain(f.Email, websiteUrl))) break;
        }

        return PickBest(allFound, websiteUrl);
    }

    public async Task<EmailDiscoveryResult> DiscoverFromExternalProfilesAsync(
        IReadOnlyList<ExternalProfile> profiles, CancellationToken ct = default)
    {
        foreach (var profile in profiles.Take(3))
        {
            var html = await FetchSafeAsync(profile.Url, ct);
            if (html is null) continue;

            var emails = EmailExtractor.ExtractAll(html);
            var best = emails.FirstOrDefault();
            if (best is not null)
            {
                var sourceType = EnrichmentScoring.GetRegistrableDomain(profile.Url) switch
                {
                    "facebook.com" => "Facebook",
                    "instagram.com" => "Instagram",
                    "linkedin.com" => "LinkedIn",
                    "pagesjaunes.fr" => "PagesJaunes",
                    var d => d ?? "Externe",
                };
                return new EmailDiscoveryResult(
                    best, profile.Url, sourceType, EmailExtractor.Classify(best), Confidence: 0.6);
            }
        }

        return new EmailDiscoveryResult(null, null, null, DiscoveredEmailKind.Unknown, 0);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void CollectEmails(string html, string sourceUrl, List<(string, string)> sink)
    {
        foreach (var email in EmailExtractor.ExtractAll(html))
            if (!sink.Any(x => x.Item1 == email))
                sink.Add((email, sourceUrl));
    }

    private static EmailDiscoveryResult PickBest(List<(string Email, string SourceUrl)> found, string websiteUrl)
    {
        if (found.Count == 0)
            return new EmailDiscoveryResult(null, null, null, DiscoveredEmailKind.Unknown, 0);

        // Priorité : même domaine que le site > générique > le reste
        var best = found
            .OrderByDescending(f => EmailExtractor.IsSameDomain(f.Email, websiteUrl))
            .ThenByDescending(f => EmailExtractor.Classify(f.Email) == DiscoveredEmailKind.Generic)
            .First();

        var kind = EmailExtractor.Classify(best.Email);
        var sameDomain = EmailExtractor.IsSameDomain(best.Email, websiteUrl);
        var confidence = (sameDomain, kind) switch
        {
            (true, DiscoveredEmailKind.Generic) => 0.95,
            (true, _) => 0.9,
            (false, DiscoveredEmailKind.Generic) => 0.75,
            _ => 0.65,
        };

        return new EmailDiscoveryResult(best.Email, best.SourceUrl, "Website", kind, confidence);
    }

    private async Task<string?> FetchSafeAsync(string url, CancellationToken ct)
    {
        try
        {
            await SsrfGuard.ValidateAsync(url, ct);
            var page = await _fetcher.FetchAsync(url, ct);
            return string.IsNullOrEmpty(page.Html) ? null : page.Html;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Page {Url} ignorée pour la découverte d'e-mail", url);
            return null;
        }
    }
}
