using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Enrichment;

public sealed record LinkedInProfile(string Url, string Kind, string Title, string Description,
    string[] Emails, string[] Phones);
public sealed record LinkedInResearch(string Kind, string Status, DateTime ObservedAt, int QueriesUsed,
    IReadOnlyList<LinkedInProfile> Profiles, string? Error);

/// <summary>Public search-index evidence only. A search match does not establish employment or identity.</summary>
public sealed class LinkedInDiscoveryService(IBraveSearchClient search, ILogger<LinkedInDiscoveryService> logger)
{
    public async Task<LinkedInResearch> DiscoverAsync(Lead lead, CancellationToken ct = default)
    {
        if (!search.IsConfigured)
            return new("linkedinSearch", "notConfigured", DateTime.UtcNow, 0, [], "Configurez la clé Brave Search pour rechercher les profils publics LinkedIn.");
        var name = string.IsNullOrWhiteSpace(lead.TradeName) ? lead.CompanyName : lead.TradeName;
        name = name.Replace('"', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();
        var tokens = EnrichmentScoring.Tokenize(name);
        if (tokens.Count == 0)
            return new("linkedinSearch", "failed", DateTime.UtcNow, 0, [], "Le nom de l’entreprise est nécessaire pour la recherche LinkedIn.");
        var queries = new[]
        {
            $"site:linkedin.com/company/ \"{name}\" {lead.City}",
            $"site:linkedin.com/in/ \"{name}\" {lead.City}",
            $"site:linkedin.com/in/ \"{name}\" (gérant OR dirigeant OR fondateur OR directeur OR owner OR CEO)"
        };
        var profiles = new Dictionary<string, LinkedInProfile>(StringComparer.OrdinalIgnoreCase);
        var attempted = 0;
        var failed = 0;
        foreach (var query in queries)
        {
            ct.ThrowIfCancellationRequested();
            attempted++;
            try
            {
                foreach (var hit in await search.SearchAsync(query, ct))
                {
                    var url = CanonicalProfileUrl(hit.Url);
                    if (url == null || profiles.ContainsKey(url) || profiles.Count >= 20) continue;
                    var title = WebUtility.HtmlDecode(hit.Title);
                    var description = WebUtility.HtmlDecode(hit.Description);
                    var evidence = EnrichmentScoring.Tokenize(title + " " + description);
                    if (tokens.Count(t => evidence.Contains(t)) < Math.Ceiling(tokens.Count * .5)) continue;
                    profiles[url] = new(url, new Uri(url).AbsolutePath.StartsWith("/company/") ? "company" : "person",
                        title[..Math.Min(title.Length, 200)], description[..Math.Min(description.Length, 1000)],
                        EmailExtractor.ExtractAll(description).Take(5).ToArray(), PhoneExtractor.Extract(description));
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                failed++;
                logger.LogWarning(ex, "Public LinkedIn search failed for lead {LeadId}", lead.Id);
            }
        }
        return new("linkedinSearch", failed == 0 ? "completed" : failed == attempted ? "failed" : "partial",
            DateTime.UtcNow, attempted, profiles.Values.ToList(),
            failed > 0 ? "Certaines recherches ont échoué. Vérifiez la configuration et le quota Brave, puis réessayez." : null);
    }

    internal static string? CanonicalProfileUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !string.IsNullOrEmpty(uri.UserInfo) || !uri.IsDefaultPort ||
            !(uri.Host == "linkedin.com" || uri.Host.EndsWith(".linkedin.com", StringComparison.OrdinalIgnoreCase))) return null;
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length != 2 || segments[0] is not ("company" or "in") || string.IsNullOrWhiteSpace(segments[1])) return null;
        return "https://www.linkedin.com/" + string.Join('/', segments);
    }

    public static string Attach(string? inspectionJson, LinkedInResearch research)
    {
        var reports = string.IsNullOrWhiteSpace(inspectionJson) ? new JsonArray() : JsonNode.Parse(inspectionJson) as JsonArray ?? new JsonArray();
        for (var i = reports.Count - 1; i >= 0; i--)
            if (reports[i]?["kind"]?.GetValue<string>() == "linkedinSearch") reports.RemoveAt(i);
        reports.Add(JsonSerializer.SerializeToNode(research, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        return reports.ToJsonString();
    }
}
