using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface IEnrichmentQueueService
{
    /// <summary>
    /// Ajoute un job d'enrichissement pour un lead. Idempotent : renvoie le job
    /// existant si un job Pending/Running existe déjà pour ce lead (sauf force).
    /// </summary>
    Task<LeadEnrichment> QueueAsync(Guid leadId, Guid? organizationId, bool force = false, CancellationToken ct = default);

    Task<List<LeadEnrichment>> GetPendingAsync(int limit = 5, CancellationToken ct = default);
    Task MarkRunningAsync(Guid jobId, CancellationToken ct = default);
    Task MarkCompletedAsync(Guid jobId, CancellationToken ct = default);
    Task MarkNeedsReviewAsync(Guid jobId, CancellationToken ct = default);
    Task MarkFailedAsync(Guid jobId, string errorMessage, bool canRetry, CancellationToken ct = default);
    Task<LeadEnrichment?> GetByIdAsync(Guid jobId, CancellationToken ct = default);
    Task<List<LeadEnrichment>> GetByLeadAsync(Guid leadId, CancellationToken ct = default);
}
