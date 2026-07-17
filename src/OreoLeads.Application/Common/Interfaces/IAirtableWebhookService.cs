namespace OreoLeads.Application.Common.Interfaces;

public interface IAirtableWebhookService
{
    Task CreateWebhookAsync(Guid configId, string notificationUrl, Guid? orgId, CancellationToken ct = default);
    Task RenewWebhookAsync(Guid configId, CancellationToken ct = default);
    Task DeleteWebhookAsync(Guid configId, CancellationToken ct = default);
    Task PollChangesAsync(Guid configId, CancellationToken ct = default);
}
