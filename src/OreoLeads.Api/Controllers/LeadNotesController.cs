using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.LeadNotes.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence.Repositories;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/leads/{leadId:guid}/notes")]
[Authorize]
public class LeadNotesController : ControllerBase
{
    private readonly LeadNoteRepository _noteRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadActivityRepository _activityRepository;
    private readonly IValidator<CreateLeadNoteDto> _createValidator;
    private readonly IValidator<UpdateLeadNoteDto> _updateValidator;

    public LeadNotesController(
        LeadNoteRepository noteRepository,
        ILeadRepository leadRepository,
        ILeadActivityRepository activityRepository,
        IValidator<CreateLeadNoteDto> createValidator,
        IValidator<UpdateLeadNoteDto> updateValidator)
    {
        _noteRepository = noteRepository;
        _leadRepository = leadRepository;
        _activityRepository = activityRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid leadId, CancellationToken ct)
    {
        if (!await _leadRepository.ExistsAsync(leadId, ct)) return NotFound();
        var notes = await _noteRepository.GetByLeadIdAsync(leadId, ct);
        return Ok(notes);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid leadId, [FromBody] CreateLeadNoteDto dto, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        if (!await _leadRepository.ExistsAsync(leadId, ct)) return NotFound();

        var note = new LeadNote
        {
            LeadId = leadId,
            Title = dto.Title,
            Content = dto.Content,
            AuthorName = dto.AuthorName
        };

        var created = await _noteRepository.CreateAsync(note, ct);

        await _activityRepository.AddAsync(new LeadActivity
        {
            LeadId = leadId,
            Type = ActivityType.NoteAdded,
            Description = $"Note ajoutée : {dto.Title}"
        }, ct);

        return Ok(new LeadNoteDto
        {
            Id = created.Id,
            LeadId = created.LeadId,
            Title = created.Title,
            Content = created.Content,
            AuthorName = created.AuthorName,
            CreatedAt = created.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid leadId, Guid id, [FromBody] UpdateLeadNoteDto dto, CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var note = await _noteRepository.GetEntityByIdAsync(id, ct);
        if (note == null || note.LeadId != leadId) return NotFound();

        note.Title = dto.Title;
        note.Content = dto.Content;
        note.SetUpdatedAt();

        await _noteRepository.UpdateAsync(note, ct);

        await _activityRepository.AddAsync(new LeadActivity
        {
            LeadId = leadId,
            Type = ActivityType.NoteUpdated,
            Description = $"Note modifiée : {dto.Title}"
        }, ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid leadId, Guid id, CancellationToken ct)
    {
        var note = await _noteRepository.GetEntityByIdAsync(id, ct);
        if (note == null || note.LeadId != leadId) return NotFound();

        await _noteRepository.SoftDeleteAsync(id, ct);

        await _activityRepository.AddAsync(new LeadActivity
        {
            LeadId = leadId,
            Type = ActivityType.NoteDeleted,
            Description = $"Note supprimée : {note.Title}"
        }, ct);

        return NoContent();
    }
}
