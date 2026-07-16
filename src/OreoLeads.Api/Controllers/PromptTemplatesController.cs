using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/prompt-templates")]
public class PromptTemplatesController : ControllerBase
{
    private readonly IPromptTemplateRepository _repo;

    public PromptTemplatesController(IPromptTemplateRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _repo.GetAllAsync();
        return Ok(templates.Select(ToDto));
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key)
    {
        var template = await _repo.GetByKeyAsync(key);
        return template is null ? NotFound() : Ok(ToDto(template));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePromptTemplateDto dto)
    {
        var all = await _repo.GetAllAsync();
        var existing = all.FirstOrDefault(t => t.Id == id);
        if (existing is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Name))
            existing.Name = dto.Name;
        if (!string.IsNullOrWhiteSpace(dto.Content))
            existing.Content = dto.Content;
        if (dto.Description is not null)
            existing.Description = dto.Description;

        existing.SetUpdatedAt();
        await _repo.UpsertAsync(existing);
        return Ok(ToDto(existing));
    }

    private static PromptTemplateDto ToDto(PromptTemplate t) => new()
    {
        Id          = t.Id,
        Name        = t.Name,
        Key         = t.Key,
        Content     = t.Content,
        Description = t.Description,
        EmailType   = t.EmailType,
        IsSystem    = t.IsSystem,
        UpdatedAt   = t.UpdatedAt ?? t.CreatedAt
    };
}
