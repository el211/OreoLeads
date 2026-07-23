using System.Text.RegularExpressions;

namespace OreoLeads.Infrastructure.Enrichment;

/// <summary>
/// Fonctions pures de scoring pour la découverte de site officiel.
/// Signaux pondérés : nom (0.35), ville/CP (0.20), téléphone (0.20),
/// SIREN/SIRET (0.15 — plancher 0.95 si présent), domaine~nom (0.10).
/// </summary>
public static class EnrichmentScoring
{
    public sealed record CompanyIdentity(
        string DisplayName,
        string? City,
        string? PostalCode,
        string? Phone,
        string? Siren,
        string? Siret);

    public sealed record ScoreResult(double Score, List<string> Signals);

    public static ScoreResult ScoreCandidate(CompanyIdentity company, string url, string pageHtml)
    {
        var signals = new List<string>();
        var score = 0.0;
        var text = StripTags(pageHtml).ToLowerInvariant();

        // Nom : proportion de tokens significatifs du nom présents dans la page
        var nameTokens = Tokenize(company.DisplayName);
        if (nameTokens.Count > 0)
        {
            var found = nameTokens.Count(t => text.Contains(t));
            var ratio = (double)found / nameTokens.Count;
            if (ratio >= 0.5)
            {
                score += 0.35 * ratio;
                signals.Add("name");
            }
        }

        // Ville ou code postal
        var cityMatch = !string.IsNullOrWhiteSpace(company.City) &&
                        text.Contains(company.City.ToLowerInvariant());
        var cpMatch = !string.IsNullOrWhiteSpace(company.PostalCode) &&
                      text.Contains(company.PostalCode);
        if (cityMatch || cpMatch)
        {
            score += 0.20;
            signals.Add(cityMatch ? "city" : "postalCode");
        }

        // Téléphone (comparaison sur chiffres uniquement)
        if (!string.IsNullOrWhiteSpace(company.Phone))
        {
            var digits = NormalizePhone(company.Phone);
            if (digits.Length >= 9 && NormalizePhone(text).Contains(digits))
            {
                score += 0.20;
                signals.Add("phone");
            }
        }

        // SIREN / SIRET dans la page (mentions légales) : signal quasi certain
        var sirenMatch = !string.IsNullOrWhiteSpace(company.Siren) &&
                         Regex.Replace(text, @"[\s.]", "").Contains(company.Siren!);
        var siretMatch = !string.IsNullOrWhiteSpace(company.Siret) &&
                         Regex.Replace(text, @"[\s.]", "").Contains(company.Siret!);
        if (sirenMatch || siretMatch)
        {
            score += 0.15;
            signals.Add(sirenMatch ? "siren" : "siret");
            score = Math.Max(score, 0.95);
        }

        // Cohérence domaine ↔ nom commercial
        if (DomainMatchesName(url, company.DisplayName))
        {
            score += 0.10;
            signals.Add("domain");
        }

        return new ScoreResult(Math.Min(score, 1.0), signals);
    }

    public static bool DomainMatchesName(string url, string companyName)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        var domain = uri.Host.Replace("www.", "").Split('.')[0].Replace("-", "");
        if (domain.Length < 4) return false;

        var tokens = Tokenize(companyName);
        if (tokens.Count == 0) return false;

        // Tous les tokens significatifs du nom apparaissent dans le domaine
        // (couvre « boulangerieducoin » vs « Boulangerie du Coin » où « du » est un mot vide).
        if (tokens.All(t => domain.Contains(t, StringComparison.OrdinalIgnoreCase)))
            return true;

        var compact = string.Concat(tokens);
        return compact.Length >= 4 &&
               (compact.Contains(domain, StringComparison.OrdinalIgnoreCase)
                || domain.Contains(compact, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Domaine enregistrable approximatif ("www.foo.pagesjaunes.fr" → "pagesjaunes.fr").</summary>
    public static string? GetRegistrableDomain(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        var parts = uri.Host.ToLowerInvariant().Split('.');
        return parts.Length < 2 ? uri.Host.ToLowerInvariant() : $"{parts[^2]}.{parts[^1]}";
    }

    public static bool IsBlacklisted(string url, IEnumerable<string> blacklist)
    {
        var domain = GetRegistrableDomain(url);
        if (domain is null) return true;
        return blacklist.Any(b =>
            domain.Equals(b, StringComparison.OrdinalIgnoreCase) ||
            domain.EndsWith("." + b, StringComparison.OrdinalIgnoreCase) ||
            // "google.com" doit aussi couvrir google.fr etc. via l'entrée dédiée
            b.Equals(domain, StringComparison.OrdinalIgnoreCase));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly string[] StopWords =
        ["le", "la", "les", "de", "du", "des", "et", "sarl", "sas", "sasu", "eurl", "ei", "sci", "monsieur", "madame"];

    internal static List<string> Tokenize(string s)
        => Regex.Split(RemoveDiacritics(s).ToLowerInvariant(), @"[^a-z0-9]+")
            .Where(t => t.Length >= 3 && !StopWords.Contains(t))
            .Distinct()
            .ToList();

    internal static string RemoveDiacritics(string s)
    {
        var normalized = s.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized.Where(c =>
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Normalize(System.Text.NormalizationForm.FormC);
    }

    private static string NormalizePhone(string s)
        => new(s.Where(char.IsDigit).ToArray());

    private static string StripTags(string html)
        => Regex.Replace(html, @"<script[^>]*>.*?</script>|<style[^>]*>.*?</style>", " ",
               RegexOptions.Singleline | RegexOptions.IgnoreCase) is var noScripts
           ? Regex.Replace(noScripts, @"<[^>]+>", " ")
           : html;
}
