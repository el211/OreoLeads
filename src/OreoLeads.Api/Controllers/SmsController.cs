using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Sms.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Api.Controllers;

[ApiController]
public class SmsController : ControllerBase
{
    // Max length accepted for an SMS. Matches the AI generator cap and the
    // frontend character counter. Brevo sends this as a concatenated (multi-part) SMS.
    private const int MaxSmsLength = 320;

    private readonly ApplicationDbContext        _db;
    private readonly ISmsQueueService            _queueSvc;
    private readonly ISmsGeneratorService        _smsGenerator;
    private readonly IBrevoConfigurationService  _brevoConfig;

    // Personal/free email domains — leads using these have no professional email
    private static readonly HashSet<string> PersonalDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "googlemail.com",
        "hotmail.com", "hotmail.fr", "outlook.com", "outlook.fr", "live.com", "live.fr", "msn.com",
        "yahoo.com", "yahoo.fr",
        "orange.fr", "wanadoo.fr",
        "sfr.fr", "neuf.fr", "cegetel.net",
        "free.fr", "laposte.net",
        "bbox.fr", "numericable.fr",
        "icloud.com", "me.com", "mac.com",
        "proton.me", "protonmail.com",
        "aol.com"
    };

    public SmsController(
        ApplicationDbContext       db,
        ISmsQueueService           queueSvc,
        ISmsGeneratorService       smsGenerator,
        IBrevoConfigurationService brevoConfig)
    {
        _db           = db;
        _queueSvc     = queueSvc;
        _smsGenerator = smsGenerator;
        _brevoConfig  = brevoConfig;
    }

    /// <summary>Generates an SMS message using AI based on the lead's website analysis.</summary>
    [HttpPost("api/leads/{leadId:guid}/generate-sms")]
    public async Task<IActionResult> GenerateSms(
        Guid leadId,
        [FromBody] GenerateSmsRequestDto dto,
        CancellationToken ct)
    {
        var lead = await _db.Set<Lead>().FindAsync([leadId], ct);
        if (lead is null) return NotFound();

        try
        {
            var result = await _smsGenerator.GenerateAsync(leadId, dto, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Queues an SMS for the given lead.
    /// Returns 202 Accepted with the job details.
    /// </summary>
    [HttpPost("api/leads/{leadId:guid}/sms")]
    public async Task<IActionResult> SendSms(
        Guid leadId,
        [FromBody] SendSmsRequestDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest("Le message SMS ne peut pas être vide.");

        if (dto.Message.Length > MaxSmsLength)
            return BadRequest($"Le message SMS ne doit pas dépasser {MaxSmsLength} caractères.");

        var lead = await _db.Set<Lead>().FindAsync([leadId], ct);
        if (lead is null) return NotFound();

        var phone = dto.ToPhone.Trim();
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest("Numéro de téléphone manquant.");

        // Fail fast if Brevo SMS isn't configured — otherwise the job would be
        // queued but silently never sent by the background worker.
        var brevoConfig = await _brevoConfig.GetCurrentAsync(ct);
        if (brevoConfig is null || !brevoConfig.IsEnabled)
            return BadRequest("L'intégration Brevo est désactivée. Activez-la dans les paramètres pour envoyer des SMS.");

        if (string.IsNullOrWhiteSpace(_brevoConfig.GetDecryptedApiKey(brevoConfig)))
            return BadRequest("La clé API Brevo est manquante. Renseignez-la dans les paramètres pour envoyer des SMS.");

        var job = await _queueSvc.QueueAsync(
            leadId:         leadId,
            toPhone:        phone,
            toName:         lead.CompanyName,
            message:        dto.Message,
            scheduledAt:    dto.ScheduledAt,
            organizationId: null,
            ct:             ct);

        return Accepted(ToDto(job));
    }

    /// <summary>Returns all SMS send jobs for a given lead.</summary>
    [HttpGet("api/leads/{leadId:guid}/sms")]
    public async Task<IActionResult> GetSmsJobs(Guid leadId, CancellationToken ct)
    {
        var jobs = await _queueSvc.GetByLeadAsync(leadId, ct);
        return Ok(jobs.Select(ToDto));
    }

    /// <summary>Returns a single SMS send job.</summary>
    [HttpGet("api/sms-jobs/{jobId:guid}")]
    public async Task<IActionResult> GetSmsJob(Guid jobId, CancellationToken ct)
    {
        var job = await _queueSvc.GetByIdAsync(jobId, ct);
        if (job is null) return NotFound();
        return Ok(ToDto(job));
    }

    /// <summary>Cancels a pending SMS send job.</summary>
    [HttpDelete("api/sms-jobs/{jobId:guid}")]
    public async Task<IActionResult> CancelJob(Guid jobId, CancellationToken ct)
    {
        var job = await _queueSvc.GetByIdAsync(jobId, ct);
        if (job is null) return NotFound();

        if (job.Status != SmsSendStatus.Pending)
            return BadRequest($"Seuls les jobs en attente peuvent être annulés. Statut actuel : {job.Status}.");

        job.Status = SmsSendStatus.Cancelled;
        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);

        return Ok(ToDto(job));
    }

    /// <summary>
    /// Returns leads that have no professional email address (no email or personal domain).
    /// Useful to identify candidates for SMS outreach.
    /// </summary>
    [HttpGet("api/leads/no-pro-email")]
    public async Task<IActionResult> GetLeadsWithoutProEmail(CancellationToken ct)
    {
        var leads = await _db.Set<Lead>()
            .Where(l => l.Phone != null && l.Phone != string.Empty)
            .Select(l => new { l.Id, l.CompanyName, l.Email, l.Phone, l.Status })
            .ToListAsync(ct);

        var result = leads
            .Where(l => IsPersonalOrNoEmail(l.Email))
            .Select(l => new
            {
                l.Id,
                l.CompanyName,
                l.Email,
                l.Phone,
                StatusValue = (int)l.Status
            });

        return Ok(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsPersonalOrNoEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return true;
        var atIdx = email.IndexOf('@');
        if (atIdx < 0) return true;
        var domain = email[(atIdx + 1)..];
        return PersonalDomains.Contains(domain);
    }

    private static SmsSendJobDto ToDto(SmsSendJob j) => new(
        Id:            j.Id,
        LeadId:        j.LeadId,
        Status:        j.Status,
        ScheduledAt:   j.ScheduledAt,
        SentAt:        j.SentAt,
        AttemptCount:  j.AttemptCount,
        MaxAttempts:   j.MaxAttempts,
        NextAttemptAt: j.NextAttemptAt,
        ErrorMessage:  j.ErrorMessage,
        BrevoMessageId: j.BrevoMessageId,
        ToPhone:       j.ToPhone,
        ToName:        j.ToName,
        Message:       j.Message,
        CreatedAt:     j.CreatedAt
    );
}
