using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public sealed record WebsiteCandidate(string Url, double Score, string Category, List<string> Signals);

public sealed record ExternalProfile(string Url, string Category);

public sealed record WebsiteDiscoveryResult(
    string? ChosenUrl,
    double Confidence,
    List<string> MatchedSignals,
    List<WebsiteCandidate> Candidates,
    List<ExternalProfile> ExternalProfiles,
    int QueriesUsed);

public interface IWebsiteDiscoveryService
{
    bool IsConfigured { get; }
    Task<WebsiteDiscoveryResult> DiscoverAsync(Lead lead, CancellationToken ct = default);
}
