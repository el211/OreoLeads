using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Analysis;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Enrichment;

/// <summary>Bounded same-origin public inspection; explicit structured people only, never guessed identities.</summary>
public sealed class ContactDiscoveryService(IPageFetcher fetcher, ApplicationDbContext db) : IContactDiscoveryService
{
    private static readonly Regex Links = new("href\\s*=\\s*[\"'](?<url>[^\"']+)[\"']", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
    private static readonly Regex JsonLd = new("<script[^>]*type\\s*=\\s*[\"']application/ld\\+json[\"'][^>]*>(?<json>.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromMilliseconds(200));

    public async Task<string> InspectAsync(Lead lead, CancellationToken ct = default, string? candidateWebsite = null)
    {
        if (!Uri.TryCreate(candidateWebsite ?? lead.Website, UriKind.Absolute, out var root)) return "[]";
        var candidateOnly = candidateWebsite != null;
        var discoveredPhones = new List<string>();
        var pending = new Queue<Uri>();
        pending.Enqueue(root);
        foreach (var path in new[] { "/contact", "/contactez-nous", "/mentions-legales" })
            pending.Enqueue(new Uri(root, path));
        var seen = new HashSet<string>();
        var reports = new List<object>();
        var contacts = await db.CompanyContacts.Where(c => c.LeadId == lead.Id).ToListAsync(ct);
        while (pending.Count > 0 && seen.Count < 8)
        {
            var url = pending.Dequeue();
            if (!seen.Add(url.AbsoluteUri)) continue;
            var page = await fetcher.FetchAsync(url.AbsoluteUri, ct);
            var observedAt = DateTime.UtcNow;
            if (page.Error != null || page.StatusCode >= 400 || page.Html == null)
            {
                reports.Add(new { sourceUrl = url.AbsoluteUri, observedAt, page.StatusCode, error = page.Error ?? $"HTTP {page.StatusCode}", candidateOnly, confidence = 1.0 });
                continue;
            }
            // Do not associate people after a cross-origin redirect.
            if (!Uri.TryCreate(page.FinalUrl, UriKind.Absolute, out var final) || final.Host != root.Host) continue;
            var html = page.Html;
            var phones = PhoneExtractor.Extract(html);
            discoveredPhones.AddRange(phones);
            var links = Links.Matches(html).Select(m => System.Net.WebUtility.HtmlDecode(m.Groups["url"].Value))
                .Select(h => Uri.TryCreate(final, h, out var u) ? u : null).Where(u => u != null).Cast<Uri>().ToList();
            var social = links.Where(u => u.Scheme == "https" && new[] { "linkedin.com", "facebook.com", "instagram.com", "x.com" }.Any(d => u.Host == d || u.Host.EndsWith("." + d))).Select(u => u.AbsoluteUri).Distinct().Take(20).ToArray();
            reports.Add(new
            {
                sourceUrl = final.AbsoluteUri, observedAt, page.StatusCode, page.ResponseTimeMs,
                viewportObserved = HtmlAnalyzer.HasViewport(html), technologiesObserved = TechnologyDetector.Detect(html),
                imagesWithoutAlt = Regex.Matches(html, "<img\\b[^>]*>", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200))
                    .Count(m => !Regex.IsMatch(m.Value, "\\balt\\s*=", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(50))),
                emails = EmailExtractor.ExtractAll(html),
                phones, candidateOnly,
                socialProfiles = social, confidence = 1.0,
                interpretation = "Static HTML observations only; absence of a signature does not prove absence of a feature. Fetch time includes robots policy checks."
            });
            foreach (Match script in JsonLd.Matches(html))
            {
                if (candidateOnly) break;
                try
                {
                    using var json = JsonDocument.Parse(script.Groups["json"].Value);
                    foreach (var person in People(json.RootElement))
                    {
                        var name = Text(person, "name");
                        var title = Text(person, "jobTitle");
                        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(title) || name.Length > 200 || title.Length > 100) continue;
                        var parts = name.Trim().Split(' ', 2);
                        if (parts.Any(p => p.Length > 100)) continue;
                        if (contacts.Any(c => string.Equals($"{c.FirstName} {c.LastName}".Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))) continue;
                        var email = Text(person, "email");
                        if (email != null && (email.Length > 200 || !EmailExtractor.ExtractAll(email).Contains(email))) email = null;
                        var profile = Text(person, "url");
                        if (!Uri.TryCreate(profile, UriKind.Absolute, out var profileUri) || profileUri.Scheme != "https") profile = null;
                        var contact = new CompanyContact
                        {
                            LeadId = lead.Id, FirstName = parts[0], LastName = parts.Length == 2 ? parts[1] : "",
                            Position = title, Email = email, ProfileUrl = profile, SourceUrl = final.AbsoluteUri,
                            Confidence = .7, VerificationStatus = "Inferred", ObservedAt = observedAt
                        };
                        // A Person on a company page is a candidate association, not independently verified employment.
                        contacts.Add(contact);
                        db.CompanyContacts.Add(contact);
                    }
                }
                catch (JsonException) { /* malformed publisher data is not evidence */ }
            }
            foreach (var link in links.Where(u => u.Scheme == root.Scheme && u.Authority == root.Authority && u.Query.Length == 0)
                .OrderByDescending(u => new[] { "contact", "about", "equipe", "team", "legal", "mentions" }.Any(k => u.AbsolutePath.Contains(k, StringComparison.OrdinalIgnoreCase))).Take(12))
            {
                var clean = new UriBuilder(link) { Fragment = "" }.Uri;
                if (pending.Count < 24 && !seen.Contains(clean.AbsoluteUri)) pending.Enqueue(clean);
            }
        }
        if (!candidateOnly && string.IsNullOrWhiteSpace(lead.Phone))
        {
            lead.Phone = discoveredPhones.Distinct().OrderByDescending(PhoneExtractor.IsFrenchMobile).FirstOrDefault();
            if (lead.Phone != null) lead.SetUpdatedAt();
        }
        return JsonSerializer.Serialize(reports, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public static IEnumerable<JsonElement> People(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Array)
            foreach (var child in node.EnumerateArray()) foreach (var person in People(child)) yield return person;
        if (node.ValueKind != JsonValueKind.Object) yield break;
        if (Text(node, "@type") == "Person") yield return node;
        foreach (var property in node.EnumerateObject())
            if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                foreach (var person in People(property.Value)) yield return person;
    }
    private static string? Text(JsonElement node, string key) => node.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
