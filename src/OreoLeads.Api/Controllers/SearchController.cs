using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Search.DTOs;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ISearchRepository _searchRepository;

    public SearchController(ISearchService searchService, ISearchRepository searchRepository)
    {
        _searchService = searchService;
        _searchRepository = searchRepository;
    }

    /// <summary>
    /// Recherche des entreprises françaises via l'API officielle (recherche-entreprises.api.gouv.fr).
    /// Détecte automatiquement les doublons avec les prospects existants.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Search([FromBody] CompanySearchRequestDto request, CancellationToken ct)
    {
        if (request.MaxResults is < 1 or > 200) request.MaxResults = 50;

        var result = await _searchService.SearchAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Importe les entreprises sélectionnées en tant que prospects.
    /// Enrichit les existants sans écraser les données manuelles.
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] SearchImportRequestDto request, CancellationToken ct)
    {
        if (request.Companies.Count == 0)
            return BadRequest(new { message = "Aucune entreprise à importer." });

        var result = await _searchService.ImportAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Historique paginé des recherches.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var result = await _searchRepository.GetHistoryAsync(page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Détail d'une recherche dans l'historique.</summary>
    [HttpGet("history/{id:guid}")]
    public async Task<IActionResult> GetHistoryEntry(Guid id, CancellationToken ct)
    {
        var entry = await _searchRepository.GetByIdAsync(id, ct);
        return entry == null ? NotFound() : Ok(entry);
    }
}
