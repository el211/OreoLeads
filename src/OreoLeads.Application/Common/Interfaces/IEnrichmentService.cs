using OreoLeads.Application.Features.Enrichment.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface IEnrichmentService
{
    Task<EnrichmentQueueResultDto> QueueAsync(Guid leadId, bool force, CancellationToken ct = default);
    Task<List<LeadEnrichmentDto>> GetByLeadAsync(Guid leadId, CancellationToken ct = default);
    Task<LeadEnrichmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Applique manuellement le site/e-mail choisi au lead et pose le verrou de validation.</summary>
    Task<LeadEnrichmentDto?> ValidateAsync(Guid id, EnrichmentValidateRequestDto request, CancellationToken ct = default);
}
