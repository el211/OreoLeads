using OreoLeads.Application.Features.Airtable.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface IAirtableSyncService
{
    Task<AirtableSyncJob> EnqueueSyncAsync(EnqueueAirtableSyncDto dto, Guid? organizationId, CancellationToken ct = default);
    Task<AirtableSyncJob?> GetJobAsync(Guid jobId, CancellationToken ct = default);
    Task<List<AirtableSyncJob>> GetRecentJobsAsync(Guid? organizationId, int limit, CancellationToken ct = default);
    Task<List<AirtableSyncLog>> GetLogsAsync(Guid jobId, CancellationToken ct = default);
    Task<List<AirtableRecordLink>> GetConflictsAsync(Guid? organizationId, CancellationToken ct = default);
    Task ResolveConflictAsync(Guid recordLinkId, ConflictResolutionDto resolution, Guid? organizationId, CancellationToken ct = default);
    Task ProcessJobAsync(Guid jobId, CancellationToken ct = default);
    Task CancelJobAsync(Guid jobId, CancellationToken ct = default);
}
