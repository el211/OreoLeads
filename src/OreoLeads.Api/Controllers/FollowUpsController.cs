using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.FollowUps.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Api.Controllers;

[ApiController]
public class FollowUpsController : ControllerBase
{
    private readonly IFollowUpRepository _followUpRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadActivityRepository _activityRepository;

    public FollowUpsController(
        IFollowUpRepository followUpRepository,
        ILeadRepository leadRepository,
        ILeadActivityRepository activityRepository)
    {
        _followUpRepository = followUpRepository;
        _leadRepository = leadRepository;
        _activityRepository = activityRepository;
    }

    /// <summary>Toutes les relances en attente</summary>
    [HttpGet("api/followups")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var followUps = await _followUpRepository.GetPendingAsync(ct);
        return Ok(followUps);
    }

    /// <summary>Relances en retard</summary>
    [HttpGet("api/followups/overdue")]
    public async Task<IActionResult> GetOverdue(CancellationToken ct)
    {
        var followUps = await _followUpRepository.GetOverdueAsync(ct);
        return Ok(followUps);
    }

    /// <summary>Relances d'un prospect</summary>
    [HttpGet("api/leads/{leadId:guid}/followups")]
    public async Task<IActionResult> GetByLead(Guid leadId, CancellationToken ct)
    {
        if (!await _leadRepository.ExistsAsync(leadId, ct)) return NotFound();
        var followUps = await _followUpRepository.GetByLeadIdAsync(leadId, ct);
        return Ok(followUps);
    }

    /// <summary>Créer une relance pour un prospect</summary>
    [HttpPost("api/leads/{leadId:guid}/followups")]
    public async Task<IActionResult> Create(Guid leadId, [FromBody] CreateFollowUpDto dto, CancellationToken ct)
    {
        if (!await _leadRepository.ExistsAsync(leadId, ct)) return NotFound();

        var followUp = new FollowUp
        {
            LeadId = leadId,
            ScheduledAt = dto.ScheduledAt,
            UserName = dto.UserName,
            Comment = dto.Comment,
            Priority = dto.Priority,
            Status = FollowUpStatus.Pending
        };

        var created = await _followUpRepository.CreateAsync(followUp, ct);

        await _activityRepository.AddAsync(new LeadActivity
        {
            LeadId = leadId,
            Type = ActivityType.FollowUpCreated,
            Description = $"Relance planifiée pour le {dto.ScheduledAt:dd/MM/yyyy}"
        }, ct);

        return Ok(new FollowUpDto
        {
            Id = created.Id,
            LeadId = created.LeadId,
            ScheduledAt = created.ScheduledAt,
            UserName = created.UserName,
            Comment = created.Comment,
            Status = created.Status,
            StatusLabel = "En attente",
            Priority = created.Priority,
            PriorityLabel = created.Priority.ToString(),
            CreatedAt = created.CreatedAt
        });
    }

    /// <summary>Mettre à jour une relance</summary>
    [HttpPut("api/followups/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFollowUpDto dto, CancellationToken ct)
    {
        var followUp = await _followUpRepository.GetEntityByIdAsync(id, ct);
        if (followUp == null) return NotFound();

        followUp.ScheduledAt = dto.ScheduledAt;
        followUp.Comment = dto.Comment;
        followUp.Status = dto.Status;
        followUp.Priority = dto.Priority;
        followUp.CompletedAt = dto.CompletedAt;
        followUp.SetUpdatedAt();

        await _followUpRepository.UpdateAsync(followUp, ct);
        return NoContent();
    }

    /// <summary>Supprimer une relance</summary>
    [HttpDelete("api/followups/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var followUp = await _followUpRepository.GetEntityByIdAsync(id, ct);
        if (followUp == null) return NotFound();
        await _followUpRepository.DeleteAsync(id, ct);
        return NoContent();
    }
}
