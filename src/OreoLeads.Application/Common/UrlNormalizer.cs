namespace OreoLeads.Application.Common;

/// <summary>
/// Normalisation et validation d'URL de site web. Un domaine nu saisi sans
/// schéma (« exemple.fr ») est accepté et complété en « https://exemple.fr ».
/// </summary>
public static class UrlNormalizer
{
    /// <summary>Ajoute https:// si le schéma manque. Renvoie la valeur telle quelle si vide.</summary>
    public static string? Normalize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        var trimmed = url.Trim();
        return trimmed.Contains("://", StringComparison.Ordinal) ? trimmed : "https://" + trimmed;
    }

    /// <summary>true si vide, ou si (une fois normalisée) l'URL est un http(s) absolu avec un domaine.</summary>
    public static bool IsValidHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(Normalize(url), UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps)
               && result.Host.Contains('.');
    }
}
