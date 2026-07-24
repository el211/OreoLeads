using OreoLeads.Application.Features.Search.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

/// <summary>
/// Traduit une requête en langage naturel en filtres de recherche d'entreprises,
/// via le fournisseur IA configuré. Ne génère PAS d'entreprises — seulement des filtres.
/// </summary>
public interface ISearchQueryParser
{
    Task<AiSearchParseResultDto> ParseAsync(string prompt, CancellationToken ct = default);
}
