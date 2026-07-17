using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Airtable.DTOs;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/airtable")]
[Authorize]
public class AirtableController : ControllerBase
{
    private readonly IAirtableConfigurationService _configSvc;
    private readonly ApplicationDbContext          _db;

    public AirtableController(
        IAirtableConfigurationService configSvc,
        ApplicationDbContext          db)
    {
        _configSvc = configSvc;
        _db        = db;
    }

    // ── GET api/airtable ──────────────────────────────────────────────────────

    /// <summary>Returns current Airtable configuration (access token never returned in clear).</summary>
    [HttpGet]
    public async Task<IActionResult> GetConfiguration(CancellationToken ct)
    {
        var orgId  = GetOrganizationId();
        var config = await _configSvc.GetCurrentAsync(orgId, ct);
        if (config is null) return Ok(DefaultDto());
        return Ok(ToDto(config));
    }

    // ── PUT api/airtable ──────────────────────────────────────────────────────

    /// <summary>Saves (upserts) Airtable configuration.</summary>
    [HttpPut]
    public async Task<IActionResult> UpdateConfiguration(
        [FromBody] UpdateAirtableConfigurationDto dto, CancellationToken ct)
    {
        var orgId  = GetOrganizationId();
        var config = await _configSvc.SaveAsync(dto, orgId, ct);
        return Ok(ToDto(config));
    }

    // ── POST api/airtable/test ────────────────────────────────────────────────

    /// <summary>Tests the connection to Airtable with the stored access token.</summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection(CancellationToken ct)
    {
        var orgId  = GetOrganizationId();
        var result = await _configSvc.TestConnectionAsync(orgId, ct);
        return Ok(result);
    }

    // ── GET api/airtable/tables ───────────────────────────────────────────────

    /// <summary>Lists all tables in the configured Airtable base.</summary>
    [HttpGet("tables")]
    public async Task<IActionResult> GetTables(CancellationToken ct)
    {
        var orgId  = GetOrganizationId();
        var tables = await _configSvc.GetTablesAsync(orgId, ct);
        return Ok(tables);
    }

    // ── GET api/airtable/fields?table=xxx ─────────────────────────────────────

    /// <summary>Lists all fields in the specified table.</summary>
    [HttpGet("fields")]
    public async Task<IActionResult> GetFields([FromQuery] string table, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(table))
            return BadRequest("Table parameter is required.");

        var orgId  = GetOrganizationId();
        var fields = await _configSvc.GetFieldsAsync(orgId, table, ct);
        return Ok(fields);
    }

    // ── GET api/airtable/mappings ─────────────────────────────────────────────

    /// <summary>Returns field mappings for the current configuration.</summary>
    [HttpGet("mappings")]
    public async Task<IActionResult> GetMappings(CancellationToken ct)
    {
        var orgId  = GetOrganizationId();
        var config = await _configSvc.GetCurrentAsync(orgId, ct);
        if (config is null) return Ok(Array.Empty<AirtableFieldMappingDto>());

        var mappings = await _configSvc.GetMappingsAsync(config.Id, ct);
        return Ok(mappings.Select(m => new AirtableFieldMappingDto(
            m.Id, m.AirtableConfigurationId, m.OreoLeadsField, m.AirtableFieldName,
            m.AirtableFieldType, m.Direction, m.IsRequired, m.DefaultValue,
            m.Transformation, m.SortOrder)));
    }

    // ── PUT api/airtable/mappings ─────────────────────────────────────────────

    /// <summary>Saves field mappings for the current configuration.</summary>
    [HttpPut("mappings")]
    public async Task<IActionResult> SaveMappings(
        [FromBody] List<SaveAirtableFieldMappingDto> mappings, CancellationToken ct)
    {
        var orgId  = GetOrganizationId();
        var config = await _configSvc.GetCurrentAsync(orgId, ct);
        if (config is null)
            return BadRequest("Airtable is not configured. Please save a configuration first.");

        await _configSvc.SaveMappingsAsync(config.Id, mappings, ct);
        return Ok();
    }

    // ── GET api/airtable/stats ────────────────────────────────────────────────

    /// <summary>Returns sync statistics.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var orgId = GetOrganizationId();

        var jobs = await _db.AirtableSyncJobs.ToListAsync(ct);

        var totalSyncs      = jobs.Count;
        var successfulSyncs = jobs.Count(j => j.Status == AirtableSyncJobStatus.Completed);
        var failedSyncs     = jobs.Count(j => j.Status == AirtableSyncJobStatus.Failed);
        var totalExported   = jobs.Where(j => j.Direction == SyncDirection.OreoLeadsToAirtable || j.Direction == SyncDirection.Bidirectional)
                                  .Sum(j => j.SuccessRecords);
        var totalImported   = jobs.Where(j => j.Direction == SyncDirection.AirtableToOreoLeads || j.Direction == SyncDirection.Bidirectional)
                                  .Sum(j => j.SuccessRecords);
        var activeConflicts = await _db.AirtableRecordLinks
                                       .CountAsync(l => l.ConflictStatus == AirtableSyncJobStatus.Conflict, ct);
        var lastSyncAt      = jobs.Where(j => j.CompletedAt.HasValue).Max(j => j.CompletedAt);

        return Ok(new AirtableSyncStatsDto(
            totalSyncs, successfulSyncs, failedSyncs,
            totalExported, totalImported, activeConflicts, lastSyncAt));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid? GetOrganizationId()
    {
        var claim = User.FindFirst("organizationId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static AirtableConfigurationDto ToDto(Domain.Entities.AirtableConfiguration c) => new(
        Id:              c.Id,
        ConnectionName:  c.ConnectionName,
        HasAccessToken:  !string.IsNullOrWhiteSpace(c.EncryptedAccessToken),
        BaseId:          c.BaseId,
        TableIdOrName:   c.TableIdOrName,
        IsEnabled:       c.IsEnabled,
        SyncDirection:   c.SyncDirection,
        ConflictStrategy:c.ConflictStrategy,
        LastSyncAt:      c.LastSyncAt,
        HasWebhook:      !string.IsNullOrWhiteSpace(c.WebhookId),
        WebhookExpiresAt:c.WebhookExpiresAt,
        CreatedAt:       c.CreatedAt,
        UpdatedAt:       c.UpdatedAt
    );

    private static AirtableConfigurationDto DefaultDto() => new(
        Id:              Guid.Empty,
        ConnectionName:  string.Empty,
        HasAccessToken:  false,
        BaseId:          string.Empty,
        TableIdOrName:   string.Empty,
        IsEnabled:       false,
        SyncDirection:   SyncDirection.OreoLeadsToAirtable,
        ConflictStrategy:ConflictStrategy.OreoLeadsWins,
        LastSyncAt:      null,
        HasWebhook:      false,
        WebhookExpiresAt:null,
        CreatedAt:       DateTime.UtcNow,
        UpdatedAt:       null
    );
}
