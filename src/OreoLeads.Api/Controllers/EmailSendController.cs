using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Brevo.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Api.Controllers;

[ApiController]
public class EmailSendController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailQueueService   _queueSvc;

    public EmailSendController(ApplicationDbContext db, IEmailQueueService queueSvc)
    {
        _db       = db;
        _queueSvc = queueSvc;
    }

    /// <summary>
    /// Queues an approved GeneratedEmail for sending.
    /// The email must have Status == Approved. Returns 202 Accepted with the job details.
    /// </summary>
    [HttpPost("api/emails/{id:guid}/send")]
    public async Task<IActionResult> QueueEmail(Guid id, [FromBody] QueueEmailRequestDto? dto, CancellationToken ct)
    {
        var email = await _db.Set<GeneratedEmail>()
            .Include(e => e.Lead)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (email is null) return NotFound();

        if (email.Status != EmailStatus.Approved)
            return BadRequest($"Only approved emails can be queued for sending. Current status: {email.Status}.");

        if (string.IsNullOrWhiteSpace(email.Lead?.Email))
            return BadRequest("The lead has no email address.");

        var scheduledAt = dto?.ScheduledAt;
        var job = await _queueSvc.QueueAsync(
            generatedEmailId: email.Id,
            leadId:           email.LeadId,
            toEmail:          email.Lead.Email,
            toName:           email.Lead.CompanyName,
            subject:          email.Subject,
            htmlBody:         email.Body,
            scheduledAt:      scheduledAt,
            organizationId:   null,
            ct:               ct);

        return Accepted(ToDto(job));
    }

    /// <summary>Returns all send jobs for a given generated email.</summary>
    [HttpGet("api/emails/{id:guid}/send-status")]
    public async Task<IActionResult> GetSendStatus(Guid id, CancellationToken ct)
    {
        var jobs = await _queueSvc.GetByLeadAsync(id, ct);
        // Filter by GeneratedEmailId
        var filtered = jobs.Where(j => j.GeneratedEmailId == id).ToList();
        return Ok(filtered.Select(ToDto));
    }

    /// <summary>Cancels a pending send job.</summary>
    [HttpDelete("api/email-jobs/{jobId:guid}")]
    public async Task<IActionResult> CancelJob(Guid jobId, CancellationToken ct)
    {
        var job = await _queueSvc.GetByIdAsync(jobId, ct);
        if (job is null) return NotFound();

        if (job.Status != EmailSendStatus.Pending)
            return BadRequest($"Only pending jobs can be cancelled. Current status: {job.Status}.");

        job.Status = EmailSendStatus.Cancelled;
        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);

        return Ok(ToDto(job));
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static EmailSendJobDto ToDto(EmailSendJob j) => new(
        Id:              j.Id,
        GeneratedEmailId: j.GeneratedEmailId,
        LeadId:          j.LeadId,
        Status:          j.Status,
        ScheduledAt:     j.ScheduledAt,
        SentAt:          j.SentAt,
        AttemptCount:    j.AttemptCount,
        MaxAttempts:     j.MaxAttempts,
        NextAttemptAt:   j.NextAttemptAt,
        ErrorMessage:    j.ErrorMessage,
        BrevoMessageId:  j.BrevoMessageId,
        ToEmail:         j.ToEmail,
        ToName:          j.ToName,
        Subject:         j.Subject,
        CreatedAt:       j.CreatedAt
    );
}
