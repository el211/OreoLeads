using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Brevo.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Brevo;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/webhooks/brevo")]
public class BrevoWebhookController : ControllerBase
{
    private readonly ApplicationDbContext       _db;
    private readonly IBrevoConfigurationService _configSvc;
    private readonly IEmailQueueService         _queueSvc;

    public BrevoWebhookController(
        ApplicationDbContext       db,
        IBrevoConfigurationService configSvc,
        IEmailQueueService         queueSvc)
    {
        _db        = db;
        _configSvc = configSvc;
        _queueSvc  = queueSvc;
    }

    /// <summary>
    /// Receives Brevo webhook events. Accepts both a single object and an array payload.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleWebhook(CancellationToken ct)
    {
        // ── 1. Validate webhook key ────────────────────────────────────────────
        var config = await _configSvc.GetCurrentAsync(ct);
        var secret = config?.WebhookSecret; // stored encrypted; decode if needed
        if (!string.IsNullOrWhiteSpace(secret))
        {
            // WebhookSecret was saved via SaveAsync which encrypts it — decrypt first
            secret = _configSvc.GetDecryptedApiKey(new BrevoConfiguration { EncryptedApiKey = secret });
        }

        var headerValue = Request.Headers["X-Brevo-Webhook-Key"].FirstOrDefault();

        try
        {
            WebhookValidator.Validate(secret, headerValue);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

        // ── 2. Parse body (single object or array) ────────────────────────────
        List<BrevoWebhookPayloadDto> payloads;
        try
        {
            using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
            payloads = doc.RootElement.ValueKind == JsonValueKind.Array
                ? JsonSerializer.Deserialize<List<BrevoWebhookPayloadDto>>(doc.RootElement.GetRawText()) ?? []
                : [JsonSerializer.Deserialize<BrevoWebhookPayloadDto>(doc.RootElement.GetRawText())!];
        }
        catch
        {
            return BadRequest("Invalid JSON payload.");
        }

        foreach (var payload in payloads)
        {
            if (payload is null) continue;
            await ProcessPayloadAsync(payload, ct);
        }

        return Ok();
    }

    // ── Processing ────────────────────────────────────────────────────────────

    private async Task ProcessPayloadAsync(BrevoWebhookPayloadDto payload, CancellationToken ct)
    {
        var eventType  = WebhookValidator.MapEventType(payload.Event);
        var occurredAt = TryParseDate(payload.Date) ?? DateTime.UtcNow;

        // ── 3. Find matching EmailSendJob by MessageId ─────────────────────────
        EmailSendJob? job = null;
        if (!string.IsNullOrWhiteSpace(payload.MessageId))
        {
            job = await _db.Set<EmailSendJob>()
                .FirstOrDefaultAsync(j => j.BrevoMessageId == payload.MessageId, ct);
        }

        // ── 4. Resolve LeadId ──────────────────────────────────────────────────
        Guid? leadId = job?.LeadId;
        if (leadId is null && !string.IsNullOrWhiteSpace(payload.LeadId) &&
            Guid.TryParse(payload.LeadId, out var parsedLeadId))
        {
            leadId = parsedLeadId;
        }
        // Also check tags for lead:<guid> pattern
        if (leadId is null && payload.Tags is not null)
        {
            foreach (var tag in payload.Tags)
            {
                if (tag.StartsWith("lead:", StringComparison.OrdinalIgnoreCase) &&
                    Guid.TryParse(tag[5..], out var tagLeadId))
                {
                    leadId = tagLeadId;
                    break;
                }
            }
        }

        // ── 5. Create EmailEvent record ────────────────────────────────────────
        _db.Set<EmailEvent>().Add(new EmailEvent
        {
            EmailSendJobId = job?.Id,
            LeadId         = leadId,
            EventType      = eventType,
            OccurredAt     = occurredAt,
            Email          = payload.Email,
            MessageId      = payload.MessageId,
            IpAddress      = payload.Ip,
            Details        = payload.Reason is not null
                ? JsonSerializer.Serialize(new { reason = payload.Reason })
                : null
        });

        // ── 6. Handle bounce / unsubscribe / spam → UnsubscribeRecord ─────────
        var isHardStop = eventType is
            EmailEventType.HardBounce or
            EmailEventType.Unsubscribed or
            EmailEventType.Spam;

        if (isHardStop && !string.IsNullOrWhiteSpace(payload.Email))
        {
            var exists = await _db.Set<UnsubscribeRecord>()
                .AnyAsync(u => u.Email == payload.Email, ct);

            if (!exists)
            {
                _db.Set<UnsubscribeRecord>().Add(new UnsubscribeRecord
                {
                    LeadId         = leadId,
                    Email          = payload.Email,
                    UnsubscribedAt = occurredAt,
                    Source         = "webhook",
                    Reason         = payload.Reason ?? payload.Event
                });
            }
        }

        // ── 7. Add LeadActivity if lead is known ───────────────────────────────
        if (leadId.HasValue)
        {
            var activityType = eventType switch
            {
                EmailEventType.Opened       => ActivityType.EmailGenerated, // closest — EmailOpened not in enum
                EmailEventType.Clicked      => ActivityType.EmailGenerated,
                EmailEventType.Sent         => ActivityType.EmailSent,
                EmailEventType.Unsubscribed => ActivityType.StatusChanged,
                _                           => (ActivityType?)null
            };

            if (activityType.HasValue)
            {
                _db.Set<LeadActivity>().Add(new LeadActivity
                {
                    LeadId      = leadId.Value,
                    Type        = activityType.Value,
                    Description = $"Brevo event: {payload.Event} for {payload.Email}"
                });
            }
        }

        // ── 8. Update EmailSendJob status if message matches ───────────────────
        if (job is not null)
        {
            if (eventType == EmailEventType.Delivered && job.Status == EmailSendStatus.Sent)
            {
                // Already marked sent by the background service — no additional state change needed
            }
            else if (eventType is EmailEventType.HardBounce or EmailEventType.Invalid or EmailEventType.Blocked)
            {
                job.Status       = EmailSendStatus.Failed;
                job.ErrorMessage = $"Brevo event: {payload.Event}. Reason: {payload.Reason}";
                job.SetUpdatedAt();
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private static DateTime? TryParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateTime.TryParse(value, out var dt) ? dt.ToUniversalTime() : null;
    }
}
