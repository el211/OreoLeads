using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Brevo;

internal sealed class SmsSendBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(10);
    private const int MaxJobsPerTick = 5;

    private readonly IServiceScopeFactory             _scopeFactory;
    private readonly ILogger<SmsSendBackgroundService> _logger;

    public SmsSendBackgroundService(
        IServiceScopeFactory              scopeFactory,
        ILogger<SmsSendBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SmsSendBackgroundService started.");

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
                _logger.LogError(ex, "Unhandled error in SmsSendBackgroundService tick.");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }

        _logger.LogInformation("SmsSendBackgroundService stopped.");
    }

    private async Task ProcessTickAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var queueSvc  = scope.ServiceProvider.GetRequiredService<ISmsQueueService>();
        var brevoSvc  = scope.ServiceProvider.GetRequiredService<IBrevoService>();
        var configSvc = scope.ServiceProvider.GetRequiredService<IBrevoConfigurationService>();
        var db        = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var brevoConfig = await configSvc.GetCurrentAsync(ct);
        if (brevoConfig is null || !brevoConfig.IsEnabled) return;

        var brevoApiKey = configSvc.GetDecryptedApiKey(brevoConfig);
        if (string.IsNullOrWhiteSpace(brevoApiKey))
        {
            _logger.LogWarning("Brevo API key is missing. Skipping SMS tick.");
            return;
        }

        var pendingJobs = await queueSvc.GetPendingAsync(MaxJobsPerTick, ct);
        if (pendingJobs.Count == 0) return;

        _logger.LogDebug("Processing {Count} pending SMS jobs.", pendingJobs.Count);

        foreach (var job in pendingJobs)
        {
            if (ct.IsCancellationRequested) break;
            await ProcessJobAsync(job, brevoSvc, brevoApiKey, queueSvc, db, ct);
        }
    }

    private async Task ProcessJobAsync(
        SmsSendJob           job,
        IBrevoService        brevoSvc,
        string               brevoApiKey,
        ISmsQueueService     queueSvc,
        ApplicationDbContext db,
        CancellationToken    ct)
    {
        await queueSvc.MarkSendingAsync(job.Id, ct);

        try
        {
            var request = new SmsSendRequest(
                ApiKey:     brevoApiKey,
                SenderName: "OreoLeads",
                ToPhone:    NormalizePhone(job.ToPhone),
                Message:    job.Message
            );

            var messageId = await brevoSvc.SendSmsAsync(request, ct);

            await queueSvc.MarkSentAsync(job.Id, messageId, ct);

            // Faire passer le lead en "SMS envoyé" s'il est encore dans une
            // phase précoce (avant d'avoir reçu un email ou d'être plus avancé).
            var lead = await db.Set<Lead>().FindAsync([job.LeadId], ct);
            if (lead is not null &&
                lead.Status != LeadStatus.EmailSent &&
                lead.Status != LeadStatus.SmsSent   &&
                lead.Status  < LeadStatus.FollowUp1)
            {
                lead.Status = LeadStatus.SmsSent;
                lead.SetUpdatedAt();
            }

            db.Set<LeadActivity>().Add(new LeadActivity
            {
                LeadId      = job.LeadId,
                Type        = ActivityType.SmsSent,
                Description = $"SMS envoyé à {job.ToPhone}: {TruncateMessage(job.Message)}"
            });

            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Job {JobId}: SMS sent to {ToPhone}. MessageId={MessageId}",
                job.Id, job.ToPhone, messageId);
        }
        catch (Exception ex)
        {
            var canRetry = job.AttemptCount + 1 < job.MaxAttempts;
            await queueSvc.MarkFailedAsync(job.Id, ex.Message, canRetry, ct);
            _logger.LogError(ex, "Job {JobId}: failed to send SMS to {ToPhone}. CanRetry={CanRetry}",
                job.Id, job.ToPhone, canRetry);
        }
    }

    private static string TruncateMessage(string msg)
        => msg.Length > 50 ? msg[..47] + "..." : msg;

    /// <summary>
    /// Normalizes a French phone number to E.164 format required by Brevo.
    /// "0612345678" → "+33612345678"
    /// Already international → returned as-is.
    /// </summary>
    private static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        // Already international: starts with + or country code
        if (phone.TrimStart().StartsWith('+'))
            return phone.Trim();

        // French local format: 10 digits starting with 0
        if (digits.Length == 10 && digits.StartsWith('0'))
            return "+33" + digits[1..];

        // Already has country code without +: e.g. "33612345678"
        if (digits.Length == 11 && digits.StartsWith("33"))
            return "+" + digits;

        // Unknown format — return as-is, let Brevo reject it with a clear error
        return phone.Trim();
    }
}
