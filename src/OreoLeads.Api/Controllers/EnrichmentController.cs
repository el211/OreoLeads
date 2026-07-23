using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Enrichment.DTOs;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrichmentController : ControllerBase
{
    private readonly IEnrichmentService _enrichment;

    public EnrichmentController(IEnrichmentService enrichment) => _enrichment = enrichment;

    /// <summary>Met en file l'enrichissement d'un lead (site + e-mail).</summary>
    [HttpPost("leads/{leadId:guid}")]
    public async Task<IActionResult> Queue(Guid leadId, [FromQuery] bool force, CancellationToken ct)
    {
        var result = await _enrichment.QueueAsync(leadId, force, ct);
        return Accepted(result);
    }

    /// <summary>Historique des enrichissements d'un lead (le plus récent d'abord).</summary>
    [HttpGet("leads/{leadId:guid}")]
    public async Task<IActionResult> GetByLead(Guid leadId, CancellationToken ct)
        => Ok(await _enrichment.GetByLeadAsync(leadId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await _enrichment.GetByIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Valide manuellement le site et/ou l'e-mail découvert.</summary>
    [HttpPost("{id:guid}/validate")]
    public async Task<IActionResult> Validate(
        Guid id, [FromBody] EnrichmentValidateRequestDto request, CancellationToken ct)
    {
        var dto = await _enrichment.ValidateAsync(id, request, ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}
