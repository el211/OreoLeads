using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/leads/{leadId:guid}/analysis")]
[Authorize]
public class WebsiteAnalysisController : ControllerBase
{
    private readonly IWebsiteAnalyzerService _analyzer;
    private readonly ILeadRepository _leadRepository;
    private readonly ApplicationDbContext _context;

    public WebsiteAnalysisController(
        IWebsiteAnalyzerService analyzer,
        ILeadRepository leadRepository,
        ApplicationDbContext context)
    {
        _analyzer = analyzer;
        _leadRepository = leadRepository;
        _context = context;
    }

    /// <summary>Récupère la dernière analyse (ou null si aucune).</summary>
    [HttpGet]
    public async Task<IActionResult> GetLatest(Guid leadId, CancellationToken ct)
    {
        var lead = await _context.Leads
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == leadId, ct);

        if (lead == null) return NotFound(new { message = "Prospect introuvable." });

        var analysis = await _context.WebsiteAnalyses
            .AsNoTracking()
            .Where(a => a.LeadId == leadId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (analysis == null) return Ok(null);

        var dto = _analyzer.ToDto(analysis, lead.Industry);
        return Ok(dto);
    }

    /// <summary>Liste l'historique des analyses pour ce prospect.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(Guid leadId, CancellationToken ct)
    {
        var analyses = await _context.WebsiteAnalyses
            .AsNoTracking()
            .Where(a => a.LeadId == leadId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var lead = await _context.Leads.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == leadId, ct);

        var dtos = analyses.Select(a => _analyzer.ToDto(a, lead?.Industry)).ToList();
        return Ok(dtos);
    }

    /// <summary>Lance une nouvelle analyse du site web du prospect.</summary>
    [HttpPost]
    public async Task<IActionResult> Analyze(Guid leadId, CancellationToken ct)
    {
        var lead = await _context.Leads
            .FirstOrDefaultAsync(l => l.Id == leadId, ct);

        if (lead == null) return NotFound(new { message = "Prospect introuvable." });

        if (string.IsNullOrWhiteSpace(lead.Website))
            return BadRequest(new { message = "Ce prospect n'a pas de site web renseigné." });

        var analysis = await _analyzer.AnalyzeAsync(leadId, lead.Website, ct);

        _context.WebsiteAnalyses.Add(analysis);
        await _context.SaveChangesAsync(ct);

        var dto = _analyzer.ToDto(analysis, lead.Industry);
        return Ok(dto);
    }

    /// <summary>Recalcule le score sans refaire la requête HTTP (utile après changement de barème).</summary>
    [HttpPost("recalculate")]
    public async Task<IActionResult> Recalculate(Guid leadId, CancellationToken ct)
    {
        var analysis = await _context.WebsiteAnalyses
            .Where(a => a.LeadId == leadId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (analysis == null)
            return NotFound(new { message = "Aucune analyse existante pour ce prospect." });

        var lead = await _context.Leads.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == leadId, ct);

        _analyzer.Recalculate(analysis);
        await _context.SaveChangesAsync(ct);

        var dto = _analyzer.ToDto(analysis, lead?.Industry);
        return Ok(dto);
    }
}
