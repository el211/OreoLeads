using OreoLeads.Application.Features.Search.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

/// <summary>
/// Interface pour les fournisseurs de données d'entreprises.
/// Ajouter un nouveau fournisseur = implémenter cette interface.
/// </summary>
public interface ILeadSource
{
    string ProviderName { get; }

    /// <summary>Taille de page brute du fournisseur (utilisée pour la pagination).</summary>
    int PageSize { get; }

    /// <summary>
    /// RawPageCount = nombre de résultats bruts de la page API avant filtres
    /// côté client — permet au SearchService de savoir s'il reste des pages.
    /// </summary>
    Task<(List<CompanySearchResultDto> Results, int TotalFound, int RawPageCount)> SearchAsync(
        CompanySearchRequestDto request,
        int page = 1,
        CancellationToken ct = default);
}
