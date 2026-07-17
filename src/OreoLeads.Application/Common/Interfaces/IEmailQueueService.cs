using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface IEmailQueueService
{
    Task<EmailSendJob> QueueAsync(
        Guid      generatedEmailId,
        Guid      leadId,
        string    toEmail,
        string?   toName,
        string    subject,
        string    htmlBody,
        DateTime? scheduledAt,
        Guid?     organizationId,
        CancellationToken ct = default);

    Task<List<EmailSendJob>> GetPendingAsync(int limit = 10, CancellationToken ct = default);

    Task MarkSendingAsync(Guid jobId, CancellationToken ct = default);

    Task MarkSentAsync(Guid jobId, string? brevoMessageId, CancellationToken ct = default);

    Task MarkFailedAsync(Guid jobId, string errorMessage, bool canRetry, CancellationToken ct = default);

    Task<EmailSendJob?> GetByIdAsync(Guid jobId, CancellationToken ct = default);

    Task<List<EmailSendJob>> GetByLeadAsync(Guid leadId, CancellationToken ct = default);
}
