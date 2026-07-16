using OreoLeads.Application.Features.Search.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

/// <summary>
/// Interface pour les fournisseurs de données d'entreprises.
/// Ajouter un nouveau fournisseur = implémenter cette interface.
/// </summary>
public interface ILeadSource
{
    string ProviderName { get; }
    Task<(List<CompanySearchResultDto> Results, int TotalFound)> SearchAsync(
        CompanySearchRequestDto request,
        int page = 1,
        CancellationToken ct = default);
}
