using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/emails")]
public class EmailDraftsController : ControllerBase
{
    private readonly IEmailDraftRepository _repo;
    private readonly IEmailGeneratorService _generator;

    public EmailDraftsController(IEmailDraftRepository repo, IEmailGeneratorService generator)
    {
        _repo      = repo;
        _generator = generator;
    }

    // ── GET /api/emails ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var result = await _repo.GetAllAsync(page, pageSize, status);
        return Ok(new
        {
            result.TotalCount,
            result.Page,
            result.PageSize,
            TotalPages = result.TotalPages,
            Items = result.Items.Select(ToDto)
        });
    }

    // ── GET /api/emails/{id} ──────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var draft = await _repo.GetByIdAsync(id);
        return draft is null ? NotFound() : Ok(ToDtoWithVersions(draft));
    }

    // ── GET /api/emails/{id}/versions ─────────────────────────────────────────

    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> GetVersions(Guid id)
    {
        var versions = await _repo.GetVersionsAsync(id);
        return Ok(versions.Select(ToVersionDto));
    }

    // ── PUT /api/emails/{id} (edit) ───────────────────────────────────────────

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmailDraftDto dto)
    {
        var draft = await _repo.GetByIdAsync(id);
        if (draft is null) return NotFound();

        if (draft.Status is EmailStatus.Approved or EmailStatus.Sent)
            return BadRequest("Un email approuvé ou envoyé ne peut plus être modifié.");

        bool changed = false;
        if (!string.IsNullOrWhiteSpace(dto.Subject) && dto.Subject != draft.Subject)
        {
            draft.Subject = dto.Subject;
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(dto.Body) && dto.Body != draft.Body)
        {
            draft.Body = dto.Body;
            changed = true;
        }

        if (changed)
        {
            draft.Status = EmailStatus.Edited;
            draft.SetUpdatedAt();
            await _repo.UpdateAsync(draft);
        }

        return Ok(ToDto(draft));
    }

    // ── POST /api/emails/{id}/approve ─────────────────────────────────────────

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var draft = await _repo.GetByIdAsync(id);
        if (draft is null) return NotFound();

        if (draft.Status == EmailStatus.Rejected)
            return BadRequest("Un email rejeté ne peut pas être approuvé directement. Régénérez-le d'abord.");

        if (draft.Status == EmailStatus.Sent)
            return BadRequest("Cet email a déjà été envoyé.");

        draft.Status = EmailStatus.Approved;
        draft.SetUpdatedAt();
        await _repo.UpdateAsync(draft);
        return Ok(ToDto(draft));
    }

    // ── POST /api/emails/{id}/reject ──────────────────────────────────────────

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var draft = await _repo.GetByIdAsync(id);
        if (draft is null) return NotFound();

        if (draft.Status == EmailStatus.Sent)
            return BadRequest("Un email envoyé ne peut pas être rejeté.");

        draft.Status = EmailStatus.Rejected;
        draft.SetUpdatedAt();
        await _repo.UpdateAsync(draft);
        return Ok(ToDto(draft));
    }

    // ── POST /api/emails/{id}/regenerate ──────────────────────────────────────

    [HttpPost("{id:guid}/regenerate")]
    public async Task<IActionResult> Regenerate(Guid id, CancellationToken ct)
    {
        var draft = await _repo.GetByIdAsync(id);
        if (draft is null) return NotFound();

        if (draft.Status == EmailStatus.Sent)
            return BadRequest("Un email envoyé ne peut pas être régénéré.");

        var version = await _generator.RegenerateAsync(id, ct);
        var updatedDraft = await _repo.GetByIdAsync(id);
        return Ok(ToDtoWithVersions(updatedDraft!));
    }

    // ── POST /api/emails/{id}/restore/{version} ───────────────────────────────

    [HttpPost("{id:guid}/restore/{version:int}")]
    public async Task<IActionResult> RestoreVersion(Guid id, int version)
    {
        var draft = await _repo.GetByIdAsync(id);
        if (draft is null) return NotFound();

        if (draft.Status == EmailStatus.Sent)
            return BadRequest("Un email envoyé ne peut pas être modifié.");

        var versions = await _repo.GetVersionsAsync(id);
        var target = versions.FirstOrDefault(v => v.Version == version);
        if (target is null) return NotFound($"Version {version} introuvable.");

        draft.Subject = target.Subject;
        draft.Body    = target.Body;
        draft.Status  = EmailStatus.Edited;
        draft.SetUpdatedAt();
        await _repo.UpdateAsync(draft);

        return Ok(ToDto(draft));
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    private static EmailDraftDto ToDto(Domain.Entities.GeneratedEmail e) => new()
    {
        Id             = e.Id,
        LeadId         = e.LeadId,
        CompanyName    = e.Lead?.CompanyName ?? string.Empty,
        Subject        = e.Subject,
        Body           = e.Body,
        Summary        = e.Summary,
        CallToAction   = e.CallToAction,
        Status         = e.Status,
        StatusLabel    = StatusLabel(e.Status),
        Style          = e.Style,
        Length         = e.Length,
        Type           = e.Type,
        TypeLabel      = TypeLabel(e.Type),
        ProviderUsed   = e.ProviderUsed,
        ModelUsed      = e.ModelUsed,
        PromptTokens   = e.PromptTokens,
        CompletionTokens = e.CompletionTokens,
        TotalTokens    = e.TotalTokens,
        GenerationMs   = e.GenerationMs,
        CurrentVersion = e.CurrentVersion,
        BusinessScore  = e.Lead?.WebsiteAnalyses?.OrderByDescending(a => a.CreatedAt)
                                                  .FirstOrDefault()?.BusinessScore,
        CreatedAt      = e.CreatedAt,
        UpdatedAt      = e.UpdatedAt
    };

    private static object ToDtoWithVersions(Domain.Entities.GeneratedEmail e) => new
    {
        Draft    = ToDto(e),
        Versions = e.Versions?.OrderByDescending(v => v.Version).Select(ToVersionDto) ?? []
    };

    private static EmailDraftVersionDto ToVersionDto(Domain.Entities.EmailDraftVersion v) => new()
    {
        Id           = v.Id,
        EmailDraftId = v.EmailDraftId,
        Version      = v.Version,
        Subject      = v.Subject,
        Body         = v.Body,
        ProviderUsed = v.ProviderUsed,
        ModelUsed    = v.ModelUsed,
        GenerationMs = v.GenerationMs,
        CreatedAt    = v.CreatedAt
    };

    private static string StatusLabel(EmailStatus s) => s switch
    {
        EmailStatus.Generated => "Généré",
        EmailStatus.Edited    => "Modifié",
        EmailStatus.Approved  => "Approuvé",
        EmailStatus.Rejected  => "Rejeté",
        EmailStatus.Sent      => "Envoyé",
        _                     => s.ToString()
    };

    private static string TypeLabel(EmailType t) => t switch
    {
        EmailType.FirstContact  => "Premier contact",
        EmailType.FollowUp      => "Relance",
        EmailType.Reply         => "Réponse",
        EmailType.Proposal      => "Proposition",
        EmailType.AfterMeeting  => "Après rendez-vous",
        EmailType.LastFollowUp  => "Dernière relance",
        _                       => t.ToString()
    };
}
