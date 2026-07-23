using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Services;
using OreoLeads.Infrastructure.Smtp;

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

        var queueSvc = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();
        var db       = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // ── Resolve sender: SMTP (own mailbox) takes priority over Brevo ──────
        var smtp = scope.ServiceProvider.GetRequiredService<SmtpEmailSender>();

        string?            brevoApiKey = null;
        BrevoConfiguration? brevoConfig = null;

        if (!smtp.IsConfigured)
        {
            var configSvc = scope.ServiceProvider.GetRequiredService<IBrevoConfigurationService>();
            brevoConfig   = await configSvc.GetCurrentAsync(ct);
            if (brevoConfig is null || !brevoConfig.IsEnabled) return;
            brevoApiKey = configSvc.GetDecryptedApiKey(brevoConfig);
            if (string.IsNullOrWhiteSpace(brevoApiKey))
            {
                _logger.LogWarning("No SMTP configured and Brevo API key is missing. Skipping email tick.");
                return;
            }
        }

        var pendingJobs = await queueSvc.GetPendingAsync(MaxJobsPerTick, ct);
        if (pendingJobs.Count == 0) return;

        _logger.LogDebug("Processing {Count} pending email jobs (provider={Provider}).",
            pendingJobs.Count, smtp.IsConfigured ? "SMTP" : "Brevo");

        var brevoSvc = smtp.IsConfigured
            ? null
            : scope.ServiceProvider.GetRequiredService<IBrevoService>();

        foreach (var job in pendingJobs)
        {
            if (ct.IsCancellationRequested) break;
            await ProcessJobAsync(job, smtp, brevoSvc, brevoConfig, brevoApiKey, queueSvc, db, ct);
        }
    }

    private async Task ProcessJobAsync(
        EmailSendJob          job,
        SmtpEmailSender       smtp,
        IBrevoService?        brevoSvc,
        BrevoConfiguration?   brevoConfig,
        string?               brevoApiKey,
        IEmailQueueService    queueSvc,
        ApplicationDbContext  db,
        CancellationToken     ct)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = job.Id });

        // TestMode (Brevo only): redirect to test address
        var toEmail = job.ToEmail;
        if (!smtp.IsConfigured && brevoConfig?.TestMode == true)
        {
            if (string.IsNullOrWhiteSpace(brevoConfig.TestModeEmail))
            {
                _logger.LogWarning("TestMode is enabled but TestModeEmail is not configured. Skipping job {JobId}.", job.Id);
                return;
            }
            toEmail = brevoConfig.TestModeEmail;
        }

        await queueSvc.MarkSendingAsync(job.Id, ct);

        try
        {
            string messageId;
            var htmlBody = EmailBodyFormatter.EnsureHtml(job.HtmlBody);

            if (smtp.IsConfigured)
            {
                // ── Send via SMTP (own mailbox) ───────────────────────────────
                messageId = await smtp.SendAsync(toEmail, job.ToName, job.Subject, htmlBody, ct);
            }
            else
            {
                // ── Send via Brevo API ────────────────────────────────────────
                var request = new EmailSendRequest(
                    ApiKey:      brevoApiKey!,
                    SenderName:  brevoConfig!.SenderName,
                    SenderEmail: brevoConfig.SenderEmail,
                    ReplyTo:     brevoConfig.ReplyTo,
                    ToEmail:     toEmail,
                    ToName:      job.ToName,
                    Subject:     job.Subject,
                    HtmlBody:    htmlBody,
                    LeadId:      job.LeadId,
                    Tags:        new[] { $"lead:{job.LeadId}" }
                );
                messageId = await brevoSvc!.SendEmailAsync(request, ct);
            }

            await queueSvc.MarkSentAsync(job.Id, messageId, ct);

            // ── Mark the GeneratedEmail as Sent ───────────────────────────────
            var genEmail = await db.Set<GeneratedEmail>()
                .FindAsync([job.GeneratedEmailId], ct);
            if (genEmail is not null)
            {
                genEmail.Status = EmailStatus.Sent;
                genEmail.SetUpdatedAt();
            }

            // ── Advance Lead status to EmailSent (if not already further along) ─
            var lead = await db.Set<Lead>().FindAsync([job.LeadId], ct);
            if (lead is not null && lead.Status is
                LeadStatus.New or LeadStatus.Qualified or
                LeadStatus.ReadyToContact or LeadStatus.EmailPrepared)
            {
                lead.Status = LeadStatus.EmailSent;
                lead.SetUpdatedAt();
            }

            db.Set<EmailEvent>().Add(new EmailEvent
            {
                EmailSendJobId = job.Id,
                LeadId         = job.LeadId,
                EventType      = EmailEventType.Sent,
                OccurredAt     = DateTime.UtcNow,
                Email          = toEmail,
                MessageId      = messageId
            });

            db.Set<LeadActivity>().Add(new LeadActivity
            {
                LeadId      = job.LeadId,
                Type        = ActivityType.EmailSent,
                Description = $"Email sent: {job.Subject}"
            });

            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Job {JobId}: email sent to {ToEmail}. MessageId={MessageId}",
                job.Id, toEmail, messageId);
        }
        catch (Exception ex)
        {
            var canRetry = job.AttemptCount + 1 < job.MaxAttempts;
            await queueSvc.MarkFailedAsync(job.Id, ex.Message, canRetry, ct);
            _logger.LogError(ex, "Job {JobId}: failed to send to {ToEmail}. CanRetry={CanRetry}",
                job.Id, toEmail, canRetry);
        }
    }
}
