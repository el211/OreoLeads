using OreoLeads.Domain.Enums;

namespace OreoLeads.Application.Common.Interfaces;

public sealed record EmailDiscoveryResult(
    string? Email,
    string? SourceUrl,
    string? SourceType,
    DiscoveredEmailKind Kind,
    double Confidence);

public interface IEmailDiscoveryService
{
    /// <summary>Recherche un e-mail public sur le site officiel d'une entreprise.</summary>
    Task<EmailDiscoveryResult> DiscoverFromWebsiteAsync(string websiteUrl, CancellationToken ct = default);

    /// <summary>
    /// Recherche un e-mail public sur des sources secondaires (Facebook, annuaires…)
    /// quand aucun site officiel n'est disponible. Best-effort : beaucoup de ces
    /// plateformes bloquent les robots.
    /// </summary>
    Task<EmailDiscoveryResult> DiscoverFromExternalProfilesAsync(
        IReadOnlyList<ExternalProfile> profiles, CancellationToken ct = default);
}
