using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;
#pragma warning disable CS8602 // Deref of possibly null reference

namespace OreoLeads.Infrastructure.Airtable;

internal sealed class AirtableSyncBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval        = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan WebhookPollInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan WebhookRenewBefore  = TimeSpan.FromHours(24);
    private const int MaxJobsPerTick = 5;

    private readonly IServiceScopeFactory                    _scopeFactory;
    private readonly ILogger<AirtableSyncBackgroundService>  _logger;

    private DateTime _lastWebhookPoll = DateTime.MinValue;

    public AirtableSyncBackgroundService(
        IServiceScopeFactory                    scopeFactory,
        ILogger<AirtableSyncBackgroundService>  logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AirtableSyncBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in AirtableSyncBackgroundService tick.");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }

        _logger.LogInformation("AirtableSyncBackgroundService stopped.");
    }

    private async Task ProcessTickAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var syncSvc    = scope.ServiceProvider.GetRequiredService<IAirtableSyncService>();
        var db         = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configSvc  = scope.ServiceProvider.GetRequiredService<IAirtableConfigurationService>();
        var webhookSvc = scope.ServiceProvider.GetRequiredService<IAirtableWebhookService>();

        // ── Process pending jobs ──────────────────────────────────────────────
        var now          = DateTime.UtcNow;
        var pendingJobs  = await db.AirtableSyncJobs
                                   .Where(j =>
                                       j.Status == AirtableSyncJobStatus.Pending &&
                                       !j.IsLocked &&
                                       (j.NextAttemptAt == null || j.NextAttemptAt <= now))
                                   .OrderBy(j => j.CreatedAt)
                                   .Take(MaxJobsPerTick)
                                   .ToListAsync(ct);

        if (pendingJobs.Count > 0)
            _logger.LogDebug("Processing {Count} pending Airtable sync jobs.", pendingJobs.Count);

        foreach (var job in pendingJobs)
        {
            if (ct.IsCancellationRequested) break;

            // Check config is enabled
            var config = await db.AirtableConfigurations
                                 .FirstOrDefaultAsync(c => c.Id == job.AirtableConfigurationId, ct);
            if (config is null || !config.IsEnabled)
            {
                _logger.LogDebug("Skipping job {JobId}: config disabled or not found.", job.Id);
                continue;
            }

            try
            {
                await syncSvc.ProcessJobAsync(job.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Airtable sync job {JobId}.", job.Id);
            }
        }

        // ── Webhook polling (every 5 minutes) ────────────────────────────────
        if (DateTime.UtcNow - _lastWebhookPoll >= WebhookPollInterval)
        {
            _lastWebhookPoll = DateTime.UtcNow;

            var configs = await db.AirtableConfigurations
                                  .Where(c => c.IsEnabled && c.WebhookId != null)
                                  .ToListAsync(ct);

            foreach (var cfg in configs)
            {
                if (ct.IsCancellationRequested) break;

                // Renew if expires within 24 hours
                if (cfg.WebhookExpiresAt.HasValue &&
                    cfg.WebhookExpiresAt.Value - DateTime.UtcNow < WebhookRenewBefore)
                {
                    try
                    {
                        await webhookSvc.RenewWebhookAsync(cfg.Id, ct);
                        _logger.LogInformation("Renewed Airtable webhook for config {ConfigId}.", cfg.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to renew Airtable webhook for config {ConfigId}.", cfg.Id);
                    }
                }

                // Poll for changes
                try
                {
                    await webhookSvc.PollChangesAsync(cfg.Id, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to poll Airtable webhook changes for config {ConfigId}.", cfg.Id);
                }
            }
        }
    }
}
