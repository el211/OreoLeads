using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Tags.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly ITagRepository _tagRepository;
    private readonly ILeadRepository _leadRepository;

    public TagsController(ITagRepository tagRepository, ILeadRepository leadRepository)
    {
        _tagRepository = tagRepository;
        _leadRepository = leadRepository;
    }

    [HttpGet("api/tags")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tags = await _tagRepository.GetAllAsync(ct);
        return Ok(tags);
    }

    [HttpPost("api/tags")]
    public async Task<IActionResult> Create([FromBody] CreateTagDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Le nom du tag est obligatoire.");

        var tag = new Tag { Name = dto.Name.Trim(), Color = dto.Color };
        var created = await _tagRepository.CreateAsync(tag, ct);
        return Ok(new TagDto { Id = created.Id, Name = created.Name, Color = created.Color });
    }

    [HttpDelete("api/tags/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tag = await _tagRepository.GetEntityByIdAsync(id, ct);
        if (tag == null) return NotFound();
        await _tagRepository.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("api/leads/{leadId:guid}/tags/{tagId:guid}")]
    public async Task<IActionResult> AddTagToLead(Guid leadId, Guid tagId, CancellationToken ct)
    {
        if (!await _leadRepository.ExistsAsync(leadId, ct)) return NotFound("Lead not found.");
        var tag = await _tagRepository.GetEntityByIdAsync(tagId, ct);
        if (tag == null) return NotFound("Tag not found.");
        await _tagRepository.AddTagToLeadAsync(leadId, tagId, ct);
        return NoContent();
    }

    [HttpDelete("api/leads/{leadId:guid}/tags/{tagId:guid}")]
    public async Task<IActionResult> RemoveTagFromLead(Guid leadId, Guid tagId, CancellationToken ct)
    {
        if (!await _leadRepository.ExistsAsync(leadId, ct)) return NotFound("Lead not found.");
        await _tagRepository.RemoveTagFromLeadAsync(leadId, tagId, ct);
        return NoContent();
    }
}
