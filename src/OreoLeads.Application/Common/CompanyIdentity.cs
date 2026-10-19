namespace OreoLeads.Application.Common;

public static class CompanyIdentity
{
    // Preserve subdomains: separate businesses can share a hosting provider.
    public static string? Domain(string? website)
    {
        if (string.IsNullOrWhiteSpace(website)) return null;
        if (!website.Contains("://")) website = "https://" + website;
        if (!Uri.TryCreate(website, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") || uri.UserInfo.Length != 0) return null;
        var host = uri.IdnHost.TrimEnd('.').ToLowerInvariant();
        return host.StartsWith("www.") ? host[4..] : host;
    }
}
