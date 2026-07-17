using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Airtable.DTOs;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/airtable/sync")]
[Authorize]
public class AirtableSyncController : ControllerBase
{
    private readonly IAirtableSyncService          _syncSvc;
    private readonly IAirtableConfigurationService _configSvc;

    public AirtableSyncController(
        IAirtableSyncService          syncSvc,
        IAirtableConfigurationService configSvc)
    {
        _syncSvc   = syncSvc;
        _configSvc = configSvc;
    }

    // ── POST api/airtable/sync ─────────────────────────────────────────────────

    /// <summary>Enqueues an incremental sync with the configured direction.</summary>
    [HttpPost]
    public async Task<IActionResult> EnqueueSync(CancellationToken ct)
    {
        var orgId  = GetOrganizationId();
        var config = await _configSvc.GetCurrentAsync(orgId, ct);
        if (config is null) return BadRequest("Airtable is not configured.");

        var job = await _syncSvc.EnqueueSyncAsync(new EnqueueAirtableSyncDto(
            AirtableConfigurationId: config.Id,
            Direction:               config.SyncDirection,
            IsFullSync:              false,
            LeadId:                  null,
            TriggerReason:           "manual"
        ), orgId, ct);

        return Ok(ToDto(job));
    }

    // ── POST api/airtable/sync/full ────────────────────────────────────────────

    /// <summary>Enqueues a full sync.</summary>
    [HttpPost("full")]
    public async Task<IActionResult> EnqueueFullSync(CancellationToken ct)
    {
        var orgId  = GetOrganizationId();
        var config = await _configSvc.GetCurrentAsync(orgId, ct);
        if (config is null) return BadRequest("Airtable is not configured.");

        var job = await _syncSvc.EnqueueSyncAsync(new EnqueueAirtableSyncDto(
            AirtableConfigurationId: config.Id,
            Direction:               config.SyncDirection,
            IsFullSync:              true,
            LeadId:                  null,
            TriggerReason:           "manual-full"
        ), orgId, ct);

        return Ok(ToDto(job));
    }

    // ── GET api/airtable/sync/jobs ─────────────────────────────────────────────

    /// <summary>Lists recent sync jobs.</summary>
    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var jobs  = await _syncSvc.GetRecentJobsAsync(orgId, limit, ct);
        return Ok(jobs.Select(ToDto));
    }

    // ── GET api/airtable/sync/jobs/{id} ───────────────────────────────────────

    /// <summary>Gets a specific sync job.</summary>
    [HttpGet("jobs/{id:guid}")]
    public async Task<IActionResult> GetJob(Guid id, CancellationToken ct)
    {
        var job = await _syncSvc.GetJobAsync(id, ct);
        if (job is null) return NotFound();
        return Ok(ToDto(job));
    }

    // ── GET api/airtable/sync/jobs/{id}/logs ──────────────────────────────────

    /// <summary>Gets logs for a specific sync job.</summary>
    [HttpGet("jobs/{id:guid}/logs")]
    public async Task<IActionResult> GetJobLogs(Guid id, CancellationToken ct)
    {
        var logs = await _syncSvc.GetLogsAsync(id, ct);
        return Ok(logs.Select(l => new AirtableSyncLogDto(
            l.Id, l.AirtableSyncJobId, l.LeadId, l.AirtableRecordId,
            l.Action, l.Details, l.ErrorMessage, l.Success, l.OccurredAt)));
    }

    // ── DELETE api/airtable/sync/jobs/{id} ────────────────────────────────────

    /// <summary>Cancels a pending sync job.</summary>
    [HttpDelete("jobs/{id:guid}")]
    public async Task<IActionResult> CancelJob(Guid id, CancellationToken ct)
    {
        await _syncSvc.CancelJobAsync(id, ct);
        return NoContent();
    }

    // ── GET api/airtable/sync/conflicts ───────────────────────────────────────

    /// <summary>Lists all active conflicts.</summary>
    [HttpGet("conflicts")]
    public async Task<IActionResult> GetConflicts(CancellationToken ct)
    {
        var orgId     = GetOrganizationId();
        var conflicts = await _syncSvc.GetConflictsAsync(orgId, ct);

        return Ok(conflicts.Select(l => new AirtableRecordLinkDto(
            l.Id,
            l.LeadId,
            l.Lead is not null ? l.Lead.CompanyName : null,
            l.AirtableConfigurationId,
            l.AirtableRecordId,
            l.LastSyncedAt,
            l.ConflictStatus,
            l.ConflictOreoLeadsData,
            l.ConflictAirtableData,
            l.ConflictDetectedAt,
            l.AirtableModifiedAt)));
    }

    // ── POST api/airtable/sync/conflicts/{id}/resolve ─────────────────────────

    /// <summary>Resolves a conflict.</summary>
    [HttpPost("conflicts/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveConflict(
        Guid id, [FromBody] ConflictResolutionDto resolution, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        await _syncSvc.ResolveConflictAsync(id, resolution, orgId, ct);
        return Ok();
    }

    // ── POST api/airtable/sync/retry-failed ───────────────────────────────────

    /// <summary>Re-enqueues all failed sync jobs for retry.</summary>
    [HttpPost("retry-failed")]
    public async Task<IActionResult> RetryFailed(CancellationToken ct)
    {
        var orgId  = GetOrganizationId();
        var config = await _configSvc.GetCurrentAsync(orgId, ct);
        if (config is null) return BadRequest("Airtable is not configured.");

        var failedJobs = await _syncSvc.GetRecentJobsAsync(orgId, 100, ct);
        var requeued   = 0;

        foreach (var job in failedJobs.Where(j => j.Status == AirtableSyncJobStatus.Failed))
        {
            await _syncSvc.EnqueueSyncAsync(new EnqueueAirtableSyncDto(
                AirtableConfigurationId: job.AirtableConfigurationId,
                Direction:               job.Direction,
                IsFullSync:              job.IsFullSync,
                LeadId:                  job.LeadId,
                TriggerReason:           "retry"
            ), orgId, ct);
            requeued++;
        }

        return Ok(new { requeued });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid? GetOrganizationId()
    {
        var claim = User.FindFirst("organizationId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static AirtableSyncJobDto ToDto(Domain.Entities.AirtableSyncJob j) => new(
        j.Id, j.AirtableConfigurationId, j.Status, j.Direction, j.TriggerReason,
        j.IsFullSync, j.LeadId, j.TotalRecords, j.ProcessedRecords, j.SuccessRecords,
        j.FailedRecords, j.ConflictRecords, j.AttemptCount, j.MaxAttempts,
        j.StartedAt, j.CompletedAt, j.NextAttemptAt, j.ErrorMessage, j.CreatedAt);
}
