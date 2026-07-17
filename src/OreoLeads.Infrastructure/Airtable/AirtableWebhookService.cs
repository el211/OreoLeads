using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Airtable.DTOs;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Airtable;

/// <summary>
/// Manages Airtable webhook lifecycle.
///
/// NOTE: Airtable webhooks use a ping-then-poll model, NOT a push model.
/// When a change occurs in an Airtable base, Airtable sends a lightweight
/// ping notification to the configured notificationUrl. The application must
/// then call the webhook payloads endpoint (GetWebhookChangesAsync) to retrieve
/// the actual change data. This is different from e.g. Brevo webhooks which
/// push full event data directly.
///
/// Webhooks expire after 7 days and must be refreshed.
/// </summary>
internal sealed class AirtableWebhookService : IAirtableWebhookService
{
    private readonly ApplicationDbContext         _db;
    private readonly IAirtableService             _airtable;
    private readonly IAirtableConfigurationService _configSvc;
    private readonly IAirtableSyncService         _syncSvc;
    private readonly ILogger<AirtableWebhookService> _logger;

    public AirtableWebhookService(
        ApplicationDbContext              db,
        IAirtableService                  airtable,
        IAirtableConfigurationService     configSvc,
        IAirtableSyncService              syncSvc,
        ILogger<AirtableWebhookService>   logger)
    {
        _db        = db;
        _airtable  = airtable;
        _configSvc = configSvc;
        _syncSvc   = syncSvc;
        _logger    = logger;
    }

    public async Task CreateWebhookAsync(
        Guid configId, string notificationUrl, Guid? orgId, CancellationToken ct = default)
    {
        var config = await _db.AirtableConfigurations.FindAsync([configId], ct);
        if (config is null) return;

        var token = _configSvc.GetDecryptedAccessToken(config);
        if (string.IsNullOrWhiteSpace(token)) return;

        var webhook = await _airtable.CreateWebhookAsync(token, config.BaseId, notificationUrl, ct);
        if (webhook is null) return;

        config.WebhookId        = webhook.Id;
        config.WebhookCursor    = webhook.Cursor;
        config.WebhookExpiresAt = webhook.ExpirationTime ?? DateTime.UtcNow.AddDays(7);
        config.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Airtable webhook {WebhookId} created for config {ConfigId}.", webhook.Id, configId);
    }

    public async Task RenewWebhookAsync(Guid configId, CancellationToken ct = default)
    {
        var config = await _db.AirtableConfigurations.FindAsync([configId], ct);
        if (config is null || string.IsNullOrWhiteSpace(config.WebhookId)) return;

        var token = _configSvc.GetDecryptedAccessToken(config);
        if (string.IsNullOrWhiteSpace(token)) return;

        await _airtable.RefreshWebhookAsync(token, config.BaseId, config.WebhookId, ct);

        config.WebhookExpiresAt = DateTime.UtcNow.AddDays(7);
        config.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Airtable webhook {WebhookId} renewed for config {ConfigId}.", config.WebhookId, configId);
    }

    public async Task DeleteWebhookAsync(Guid configId, CancellationToken ct = default)
    {
        var config = await _db.AirtableConfigurations.FindAsync([configId], ct);
        if (config is null || string.IsNullOrWhiteSpace(config.WebhookId)) return;

        var token = _configSvc.GetDecryptedAccessToken(config);
        if (string.IsNullOrWhiteSpace(token)) return;

        await _airtable.DeleteWebhookAsync(token, config.BaseId, config.WebhookId, ct);

        config.WebhookId        = null;
        config.WebhookCursor    = null;
        config.WebhookExpiresAt = null;
        config.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Airtable webhook deleted for config {ConfigId}.", configId);
    }

    /// <summary>
    /// Polls Airtable for changes since the last cursor and enqueues sync jobs
    /// for affected records. Called after receiving a ping notification.
    /// </summary>
    public async Task PollChangesAsync(Guid configId, CancellationToken ct = default)
    {
        var config = await _db.AirtableConfigurations.FindAsync([configId], ct);
        if (config is null || string.IsNullOrWhiteSpace(config.WebhookId)) return;

        var token = _configSvc.GetDecryptedAccessToken(config);
        if (string.IsNullOrWhiteSpace(token)) return;

        try
        {
            var changes = await _airtable.GetWebhookChangesAsync(
                token, config.BaseId, config.WebhookId, config.WebhookCursor, ct);

            if (changes.Changes.Count > 0)
            {
                // Enqueue a sync job for changed records
                await _syncSvc.EnqueueSyncAsync(new EnqueueAirtableSyncDto(
                    AirtableConfigurationId: config.Id,
                    Direction:   SyncDirection.AirtableToOreoLeads,
                    IsFullSync:  false,
                    LeadId:      null,
                    TriggerReason: "webhook"
                ), config.OrganizationId, ct);

                _logger.LogInformation(
                    "Airtable webhook: {Count} changes detected for config {ConfigId}, sync enqueued.",
                    changes.Changes.Count, configId);
            }

            // Update cursor
            config.WebhookCursor = changes.Cursor;
            config.SetUpdatedAt();
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling Airtable webhook changes for config {ConfigId}.", configId);
        }
    }
}
