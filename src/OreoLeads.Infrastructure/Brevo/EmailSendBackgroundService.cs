using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Brevo;

internal sealed class EmailSendBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);
    private const int MaxJobsPerTick = 10;

    private readonly IServiceScopeFactory          _scopeFactory;
    private readonly ILogger<EmailSendBackgroundService> _logger;

    public EmailSendBackgroundService(
        IServiceScopeFactory                 scopeFactory,
        ILogger<EmailSendBackgroundService>  logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailSendBackgroundService started.");

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
                _logger.LogError(ex, "Unhandled error in EmailSendBackgroundService tick.");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }

        _logger.LogInformation("EmailSendBackgroundService stopped.");
    }

    private async Task ProcessTickAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var configSvc = scope.ServiceProvider.GetRequiredService<IBrevoConfigurationService>();
        var queueSvc  = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();
        var brevoSvc  = scope.ServiceProvider.GetRequiredService<IBrevoService>();
        var db        = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var config = await configSvc.GetCurrentAsync(ct);
        if (config is null || !config.IsEnabled)
            return;

        var apiKey = configSvc.GetDecryptedApiKey(config);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Brevo API key is not configured or could not be decrypted.");
            return;
        }

        var pendingJobs = await queueSvc.GetPendingAsync(MaxJobsPerTick, ct);
        if (pendingJobs.Count == 0) return;

        _logger.LogDebug("Processing {Count} pending email jobs.", pendingJobs.Count);

        foreach (var job in pendingJobs)
        {
            if (ct.IsCancellationRequested) break;
            await ProcessJobAsync(job, config, apiKey, queueSvc, brevoSvc, db, ct);
        }
    }

    private async Task ProcessJobAsync(
        EmailSendJob          job,
        BrevoConfiguration    config,
        string                apiKey,
        IEmailQueueService    queueSvc,
        IBrevoService         brevoSvc,
        ApplicationDbContext  db,
        CancellationToken     ct)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = job.Id });

        // TestMode: redirect to test address
        var toEmail = job.ToEmail;
        if (config.TestMode)
        {
            if (string.IsNullOrWhiteSpace(config.TestModeEmail))
            {
                _logger.LogWarning(
                    "TestMode is enabled but TestModeEmail is not configured. Skipping job {JobId}.", job.Id);
                return;
            }
            _logger.LogInformation(
                "TestMode: redirecting email from {Original} to {TestEmail}.", toEmail, config.TestModeEmail);
            toEmail = config.TestModeEmail;
        }

        await queueSvc.MarkSendingAsync(job.Id, ct);

        try
        {
            var request = new EmailSendRequest(
                ApiKey:      apiKey,
                SenderName:  config.SenderName,
                SenderEmail: config.SenderEmail,
                ReplyTo:     config.ReplyTo,
                ToEmail:     toEmail,
                ToName:      job.ToName,
                Subject:     job.Subject,
                HtmlBody:    job.HtmlBody,
                LeadId:      job.LeadId,
                Tags:        new[] { $"lead:{job.LeadId}" }
            );

            var messageId = await brevoSvc.SendEmailAsync(request, ct);

            await queueSvc.MarkSentAsync(job.Id, messageId, ct);

            // Record EmailEvent
            db.Set<EmailEvent>().Add(new EmailEvent
            {
                EmailSendJobId = job.Id,
                LeadId         = job.LeadId,
                EventType      = EmailEventType.Sent,
                OccurredAt     = DateTime.UtcNow,
                Email          = toEmail,
                MessageId      = messageId
            });

            // Record LeadActivity
            db.Set<LeadActivity>().Add(new LeadActivity
            {
                LeadId      = job.LeadId,
                Type        = ActivityType.EmailSent,
                Description = $"Email sent: {job.Subject}"
            });

            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Job {JobId}: email sent to {ToEmail}. MessageId={MessageId}",
                job.Id, toEmail, messageId);
        }
        catch (Exception ex)
        {
            var canRetry = job.AttemptCount + 1 < job.MaxAttempts;
            await queueSvc.MarkFailedAsync(job.Id, ex.Message, canRetry, ct);

            _logger.LogError(
                ex,
                "Job {JobId}: failed to send email to {ToEmail}. CanRetry={CanRetry}",
                job.Id, toEmail, canRetry);
        }
    }
}
