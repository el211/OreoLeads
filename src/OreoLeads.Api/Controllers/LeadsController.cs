using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Application.Features.Leads.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence.Repositories;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeadsController : ControllerBase
{
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadActivityRepository _activityRepository;
    private readonly ITagRepository _tagRepository;
    private readonly ICsvImportService _csvImportService;
    private readonly IExcelExportService _exportService;
    private readonly IValidator<CreateLeadDto> _createValidator;
    private readonly IValidator<UpdateLeadDto> _updateValidator;
    private readonly LeadNoteRepository _noteRepository;

    public LeadsController(
        ILeadRepository leadRepository,
        ILeadActivityRepository activityRepository,
        ITagRepository tagRepository,
        ICsvImportService csvImportService,
        IExcelExportService exportService,
        IValidator<CreateLeadDto> createValidator,
        IValidator<UpdateLeadDto> updateValidator,
        LeadNoteRepository noteRepository)
    {
        _leadRepository = leadRepository;
        _activityRepository = activityRepository;
        _tagRepository = tagRepository;
        _csvImportService = csvImportService;
        _exportService = exportService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _noteRepository = noteRepository;
    }

    /// <summary>Recherche et liste les prospects avec filtres et pagination</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] LeadFilterDto filter, CancellationToken ct)
    {
        var result = await _leadRepository.SearchAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>Alias de recherche</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] LeadFilterDto filter, CancellationToken ct)
        => await GetAll(filter, ct);

    /// <summary>Obtenir un prospect par son Id</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var lead = await _leadRepository.GetByIdAsync(id, ct);
        return lead == null ? NotFound() : Ok(lead);
    }

    /// <summary>Créer un nouveau prospect</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeadDto dto, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var lead = new Lead
        {
            CompanyName = dto.CompanyName,
            TradeName = dto.TradeName,
            Industry = dto.Industry,
            Description = dto.Description,
            Website = UrlNormalizer.Normalize(dto.Website),
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            PostalCode = dto.PostalCode,
            City = dto.City,
            Department = dto.Department,
            Region = dto.Region,
            Country = dto.Country,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Siren = dto.Siren,
            Siret = dto.Siret,
            NafCode = dto.NafCode,
            EmployeeCount = dto.EmployeeCount,
            Status = dto.Status,
            Priority = dto.Priority,
            Score = dto.Score,
            AssignedUserId = dto.AssignedUserId
        };

        var created = await _leadRepository.CreateAsync(lead, ct);

        // Attach tags
        foreach (var tagId in dto.TagIds)
            await _tagRepository.AddTagToLeadAsync(created.Id, tagId, ct);

        // Log activity
        var authorName = User.FindFirstValue(ClaimTypes.Name) ?? "Inconnu";
        var authorId   = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _activityRepository.AddAsync(new LeadActivity
        {
            LeadId = created.Id,
            Type = ActivityType.Created,
            Description = $"Prospect créé : {created.CompanyName}"
        }, ct);

        // Auto-note: who added this prospect
        await _noteRepository.CreateAsync(new LeadNote
        {
            LeadId     = created.Id,
            Title      = $"Ajouté par {authorName}",
            Content    = $"Prospect ajouté par {authorName}.",
            AuthorId   = authorId != null && Guid.TryParse(authorId, out var aid) ? aid : null,
            AuthorName = authorName,
        }, ct);

        var result = await _leadRepository.GetByIdAsync(created.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, result);
    }

    /// <summary>Modifier un prospect</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadDto dto, CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var existing = await _leadRepository.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();

        var previousStatus = existing.Status;

        // Load tracked entity and update its properties
        var lead = await _leadRepository.GetEntityByIdAsync(id, ct);
        if (lead == null) return NotFound();

        lead.CompanyName = dto.CompanyName;
        lead.TradeName = dto.TradeName;
        lead.Industry = dto.Industry;
        lead.Description = dto.Description;
        lead.Website = UrlNormalizer.Normalize(dto.Website);
        lead.Email = dto.Email;
        lead.Phone = dto.Phone;
        lead.Address = dto.Address;
        lead.PostalCode = dto.PostalCode;
        lead.City = dto.City;
        lead.Department = dto.Department;
        lead.Region = dto.Region;
        lead.Country = dto.Country;
        lead.Latitude = dto.Latitude;
        lead.Longitude = dto.Longitude;
        lead.Siren = dto.Siren;
        lead.Siret = dto.Siret;
        lead.NafCode = dto.NafCode;
        lead.EmployeeCount = dto.EmployeeCount;
        lead.Status = dto.Status;
        lead.Priority = dto.Priority;
        lead.Score = dto.Score;
        lead.AssignedUserId = dto.AssignedUserId;
        lead.SetUpdatedAt();

        await _leadRepository.UpdateAsync(lead, ct);

        // Sync tags
        var currentTagIds = lead.LeadTags.Select(lt => lt.TagId).ToList();
        foreach (var tagId in currentTagIds.Except(dto.TagIds))
            await _tagRepository.RemoveTagFromLeadAsync(id, tagId, ct);
        foreach (var tagId in dto.TagIds.Except(currentTagIds))
            await _tagRepository.AddTagToLeadAsync(id, tagId, ct);

        // Log activities
        if (previousStatus != dto.Status)
        {
            await _activityRepository.AddAsync(new LeadActivity
            {
                LeadId = id,
                Type = ActivityType.StatusChanged,
                Description = $"Statut changé : {GetStatusLabel(previousStatus)} → {GetStatusLabel(dto.Status)}"
            }, ct);
        }

        await _activityRepository.AddAsync(new LeadActivity
        {
            LeadId = id,
            Type = ActivityType.Updated,
            Description = $"Prospect mis à jour : {dto.CompanyName}"
        }, ct);

        return NoContent();
    }

    /// <summary>Supprimer un prospect</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!await _leadRepository.ExistsAsync(id, ct)) return NotFound();
        await _leadRepository.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Met à jour le secteur d'activité de plusieurs prospects en une fois.</summary>
    [HttpPatch("bulk/industry")]
    public async Task<IActionResult> BulkUpdateIndustry([FromBody] BulkUpdateIndustryDto dto, CancellationToken ct)
    {
        if (dto.LeadIds is null || dto.LeadIds.Count == 0)
            return BadRequest("Aucun prospect sélectionné.");

        var updated = await _leadRepository.BulkUpdateIndustryAsync(dto.LeadIds, dto.Industry, ct);
        return Ok(new { updated });
    }

    /// <summary>Renseigne le secteur des prospects sélectionnés via l'IA (nom + description + NAF).</summary>
    [HttpPost("bulk/autofill-industry")]
    public async Task<IActionResult> AutofillIndustry(
        [FromBody] BulkAutofillIndustryDto dto,
        [FromServices] IIndustryClassifier classifier,
        CancellationToken ct)
    {
        if (dto.LeadIds is null || dto.LeadIds.Count == 0)
            return BadRequest("Aucun prospect sélectionné.");

        try
        {
            var updated = await classifier.AutofillAsync(dto.LeadIds, ct);
            return Ok(new { updated });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Nombre de prospects sans e-mail envoyé (pour la confirmation).</summary>
    [HttpGet("uncontacted/count")]
    public async Task<IActionResult> CountUncontacted(CancellationToken ct)
        => Ok(new { count = await _leadRepository.CountUncontactedAsync(ct) });

    /// <summary>
    /// Supprime tous les prospects sans e-mail envoyé. Issus de la recherche
    /// officielle, ils réapparaîtront si l'utilisateur relance la recherche.
    /// </summary>
    [HttpDelete("uncontacted")]
    public async Task<IActionResult> DeleteUncontacted(CancellationToken ct)
    {
        var deleted = await _leadRepository.DeleteUncontactedAsync(ct);
        return Ok(new { deleted });
    }

    /// <summary>Importer des prospects via CSV</summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Fichier CSV requis.");

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Le fichier doit être au format CSV.");

        await using var stream = file.OpenReadStream();
        var rows = await _csvImportService.ParseAsync(stream, ct);

        if (rows.Count == 0)
            return BadRequest("Aucune ligne valide trouvée dans le fichier CSV.");

        var leads = rows.Select(_csvImportService.MapToLead).ToList();
        var count = await _leadRepository.BulkImportAsync(leads, ct);

        return Ok(new { Imported = count, Total = rows.Count });
    }

    /// <summary>Exporter les prospects en CSV</summary>
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] LeadFilterDto filter, CancellationToken ct)
    {
        var leads = await _leadRepository.GetByFilterForExportAsync(filter, ct);
        var bytes = await _exportService.ExportLeadsToCsvAsync(leads, ct);

        await _activityRepository.AddAsync(new LeadActivity
        {
            LeadId = Guid.Empty,
            Type = ActivityType.Export,
            Description = $"Export CSV de {leads.Count} prospects"
        }, ct);

        return File(bytes, "text/csv", $"prospects_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    /// <summary>Exporter les prospects en Excel</summary>
    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] LeadFilterDto filter, CancellationToken ct)
    {
        var leads = await _leadRepository.GetByFilterForExportAsync(filter, ct);
        var bytes = await _exportService.ExportLeadsToExcelAsync(leads, ct);

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"prospects_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
    }

    /// <summary>Génère un brouillon d'email IA pour ce prospect.</summary>
    [HttpPost("{id:guid}/generate-email")]
    public async Task<IActionResult> GenerateEmail(
        Guid id,
        [FromBody] GenerateEmailRequestDto request,
        [FromServices] IEmailGeneratorService generator,
        [FromServices] ILeadRepository leadRepository,
        CancellationToken ct)
    {
        var lead = await leadRepository.GetByIdAsync(id);
        if (lead is null) return NotFound();

        var draft = await generator.GenerateAsync(id, request, ct);
        return Ok(new
        {
            draft.Id,
            draft.Subject,
            draft.Body,
            draft.Summary,
            draft.CallToAction,
            Status      = draft.Status.ToString(),
            StatusLabel = "Généré",
            draft.ProviderUsed,
            draft.ModelUsed,
            draft.TotalTokens,
            draft.GenerationMs
        });
    }

    /// <summary>Returns distinct industries and detected legal forms for filter dropdowns</summary>
    [HttpGet("meta")]
    public async Task<IActionResult> GetMeta(CancellationToken ct)
        => Ok(await _leadRepository.GetMetaAsync(ct));

    private static string GetStatusLabel(LeadStatus s) => s switch
    {
        LeadStatus.New => "Nouveau",
        LeadStatus.Qualified => "Qualifié",
        LeadStatus.ReadyToContact => "Prêt à contacter",
        LeadStatus.EmailPrepared => "Email préparé",
        LeadStatus.EmailSent => "Email envoyé",
        LeadStatus.FollowUp1 => "Relance 1",
        LeadStatus.FollowUp2 => "Relance 2",
        LeadStatus.Meeting => "Rendez-vous",
        LeadStatus.ProposalSent => "Devis envoyé",
        LeadStatus.Client => "Client",
        LeadStatus.Rejected => "Refusé",
        LeadStatus.DoNotContact => "Ne pas contacter",
        _ => s.ToString()
    };
}
