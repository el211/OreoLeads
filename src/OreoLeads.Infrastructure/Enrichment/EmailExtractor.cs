using System.Net;
using System.Text.RegularExpressions;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Enrichment;

/// <summary>
/// Extraction d'adresses e-mail publiques depuis le HTML d'une page :
/// liens mailto:, texte visible, JSON-LD, formes obfusquées
/// (« contact [at] domaine.fr », « (arobase) », entités HTML).
/// Ne devine JAMAIS d'adresse — extrait uniquement ce qui est présent.
/// </summary>
public static partial class EmailExtractor
{
    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.None)]
    private static partial Regex PlainEmailRegex();

    [GeneratedRegex(@"([a-zA-Z0-9._%+\-]+)\s*[\[\(]\s*(?:at|arobase)\s*[\]\)]\s*([a-zA-Z0-9.\-]+)\s*(?:[\[\(]\s*(?:dot|point)\s*[\]\)]\s*([a-zA-Z]{2,}))?",
        RegexOptions.IgnoreCase)]
    private static partial Regex ObfuscatedRegex();

    private static readonly string[] GenericPrefixes =
        ["contact", "info", "bonjour", "hello", "accueil", "commercial", "direction", "administration", "secretariat", "reservation"];

    private static readonly string[] NoisePatterns =
        ["no-reply", "noreply", "example.com", "exemple.fr", "sentry", "wixpress", "@2x", ".png", ".jpg", ".webp", ".svg",
         "domain.com", "email.com", "yourdomain", "votredomaine", "schema.org", "w3.org"];

    public static List<string> ExtractAll(string html)
    {
        var found = new List<string>();

        // 1. HTML brut décodé (couvre &#64; et autres entités) : mailto, texte, JSON-LD, attributs
        var decoded = WebUtility.HtmlDecode(html);
        found.AddRange(PlainEmailRegex().Matches(decoded).Select(m => m.Value));

        // 2. Formes obfusquées « contact [at] domaine [dot] fr »
        foreach (Match m in ObfuscatedRegex().Matches(decoded))
        {
            var local = m.Groups[1].Value;
            var domain = m.Groups[2].Value;
            var tld = m.Groups[3].Success ? m.Groups[3].Value : null;
            var email = tld is null ? $"{local}@{domain}" : $"{local}@{domain}.{tld}";
            if (PlainEmailRegex().IsMatch(email)) found.Add(email);
        }

        return found
            .Select(e => e.Trim().TrimEnd('.').ToLowerInvariant())
            .Where(IsPlausible)
            .Distinct()
            .ToList();
    }

    public static DiscoveredEmailKind Classify(string email)
    {
        var local = email.Split('@')[0];

        if (GenericPrefixes.Any(p => local.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return DiscoveredEmailKind.Generic;

        // prenom.nom@ ou prenom-nom@ → nominative
        if (Regex.IsMatch(local, @"^[a-z]{2,}[.\-_][a-z]{2,}$"))
            return DiscoveredEmailKind.Nominative;

        return DiscoveredEmailKind.Other;
    }

    public static bool IsSameDomain(string email, string websiteUrl)
    {
        if (!Uri.TryCreate(websiteUrl, UriKind.Absolute, out var uri)) return false;
        var emailDomain = email.Split('@').Last();
        var siteDomain = uri.Host.Replace("www.", "");
        return emailDomain.Equals(siteDomain, StringComparison.OrdinalIgnoreCase) ||
               siteDomain.EndsWith("." + emailDomain, StringComparison.OrdinalIgnoreCase) ||
               emailDomain.EndsWith("." + siteDomain, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlausible(string email)
    {
        if (email.Length is < 6 or > 80) return false;
        if (NoisePatterns.Any(n => email.Contains(n, StringComparison.OrdinalIgnoreCase))) return false;
        // Les domaines à TLD purement numérique ou trop courts sont du bruit
        var tld = email.Split('.').Last();
        return tld.Length >= 2 && tld.All(char.IsLetter);
    }
}
