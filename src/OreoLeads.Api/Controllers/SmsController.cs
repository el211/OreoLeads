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
    private readonly ApplicationDbContext _db;
    private readonly ISmsQueueService     _queueSvc;

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

    public SmsController(ApplicationDbContext db, ISmsQueueService queueSvc)
    {
        _db       = db;
        _queueSvc = queueSvc;
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

        if (dto.Message.Length > 160)
            return BadRequest("Le message SMS ne doit pas dépasser 160 caractères.");

        var lead = await _db.Set<Lead>().FindAsync([leadId], ct);
        if (lead is null) return NotFound();

        var phone = dto.ToPhone.Trim();
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest("Numéro de téléphone manquant.");

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
