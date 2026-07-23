namespace OreoLeads.Application.Common.Interfaces;

public sealed record WebSearchResult(string Url, string Title, string Description);

/// <summary>Client de recherche web (Brave Search API) pour la découverte de sites.</summary>
public interface IBraveSearchClient
{
    bool IsConfigured { get; }
    Task<List<WebSearchResult>> SearchAsync(string query, CancellationToken ct = default);
}
