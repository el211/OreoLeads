using OreoLeads.Application.Features.WebsiteAnalysis.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface IWebsiteAnalyzerService
{
    /// <summary>Analyse complète d'un site : HTTP, HTML, technologies, score, recommandations.</summary>
    Task<WebsiteAnalysis> AnalyzeAsync(Guid leadId, string url, CancellationToken ct = default);

    /// <summary>Recalcule le score et les recommandations sans refaire la requête HTTP.</summary>
    WebsiteAnalysis Recalculate(WebsiteAnalysis existing);

    /// <summary>Mappe une entité vers un DTO enrichi (désérialise JSON, génère OreoServices).</summary>
    WebsiteAnalysisDto ToDto(WebsiteAnalysis analysis, string? industry = null);
}
