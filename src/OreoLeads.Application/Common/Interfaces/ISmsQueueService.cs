using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface ISmsQueueService
{
    Task<SmsSendJob> QueueAsync(
        Guid      leadId,
        string    toPhone,
        string?   toName,
        string    message,
        DateTime? scheduledAt,
        Guid?     organizationId,
        CancellationToken ct = default);

    Task<List<SmsSendJob>> GetPendingAsync(int limit = 10, CancellationToken ct = default);
    Task MarkSendingAsync(Guid jobId, CancellationToken ct = default);
    Task MarkSentAsync(Guid jobId, string? brevoMessageId, CancellationToken ct = default);
    Task MarkFailedAsync(Guid jobId, string errorMessage, bool canRetry, CancellationToken ct = default);
    Task<SmsSendJob?> GetByIdAsync(Guid jobId, CancellationToken ct = default);
    Task<List<SmsSendJob>> GetByLeadAsync(Guid leadId, CancellationToken ct = default);
}
