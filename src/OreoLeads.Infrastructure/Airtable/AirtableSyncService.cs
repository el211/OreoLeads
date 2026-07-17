using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Airtable.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Airtable;

internal sealed class AirtableSyncService : IAirtableSyncService
{
    private readonly ApplicationDbContext         _db;
    private readonly IAirtableConfigurationService _configSvc;
    private readonly IAirtableService             _airtable;
    private readonly ILogger<AirtableSyncService> _logger;

    public AirtableSyncService(
        ApplicationDbContext          db,
        IAirtableConfigurationService configSvc,
        IAirtableService              airtable,
        ILogger<AirtableSyncService>  logger)
    {
        _db        = db;
        _configSvc = configSvc;
        _airtable  = airtable;
        _logger    = logger;
    }

    // ── Queue / query ─────────────────────────────────────────────────────────

    public async Task<AirtableSyncJob> EnqueueSyncAsync(
        EnqueueAirtableSyncDto dto, Guid? organizationId, CancellationToken ct = default)
    {
        var job = new AirtableSyncJob
        {
            AirtableConfigurationId = dto.AirtableConfigurationId,
            OrganizationId          = organizationId,
            Status                  = AirtableSyncJobStatus.Pending,
            Direction               = dto.Direction,
            TriggerReason           = dto.TriggerReason,
            IsFullSync              = dto.IsFullSync,
            LeadId                  = dto.LeadId,
            NextAttemptAt           = DateTime.UtcNow,
        };
        _db.AirtableSyncJobs.Add(job);
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<AirtableSyncJob?> GetJobAsync(Guid jobId, CancellationToken ct = default)
        => await _db.AirtableSyncJobs
                    .Include(j => j.Logs)
                    .FirstOrDefaultAsync(j => j.Id == jobId, ct);

    public async Task<List<AirtableSyncJob>> GetRecentJobsAsync(
        Guid? organizationId, int limit, CancellationToken ct = default)
        => await _db.AirtableSyncJobs
                    .OrderByDescending(j => j.CreatedAt)
                    .Take(limit)
                    .ToListAsync(ct);

    public async Task<List<AirtableSyncLog>> GetLogsAsync(
        Guid jobId, CancellationToken ct = default)
        => await _db.AirtableSyncLogs
                    .Where(l => l.AirtableSyncJobId == jobId)
                    .OrderBy(l => l.OccurredAt)
                    .ToListAsync(ct);

    public async Task<List<AirtableRecordLink>> GetConflictsAsync(
        Guid? organizationId, CancellationToken ct = default)
        => await _db.AirtableRecordLinks
                    .Include(l => l.Lead)
                    .Where(l => l.ConflictStatus == AirtableSyncJobStatus.Conflict)
                    .ToListAsync(ct);

    public async Task CancelJobAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _db.AirtableSyncJobs.FindAsync([jobId], ct);
        if (job is null) return;

        job.Status   = AirtableSyncJobStatus.Cancelled;
        job.IsLocked = false;
        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    // ── Conflict resolution ───────────────────────────────────────────────────

    public async Task ResolveConflictAsync(
        Guid recordLinkId, ConflictResolutionDto resolution,
        Guid? organizationId, CancellationToken ct = default)
    {
        var link = await _db.AirtableRecordLinks
                            .Include(l => l.Lead)
                            .FirstOrDefaultAsync(l => l.Id == recordLinkId, ct);
        if (link is null) return;

        if (resolution.WinnerSource == "airtable" &&
            !string.IsNullOrWhiteSpace(link.ConflictAirtableData) &&
            link.Lead is not null)
        {
            // Apply Airtable data to lead
            var fields = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                link.ConflictAirtableData) ?? new Dictionary<string, object?>();

            var config = await _db.AirtableConfigurations
                                  .Include(c => c.FieldMappings)
                                  .FirstOrDefaultAsync(c => c.Id == link.AirtableConfigurationId, ct);

            if (config is not null)
            {
                AirtableFieldMapper.MapAirtableFieldsToLead(fields, config.FieldMappings.ToList(), link.Lead);
                link.Lead.SetUpdatedAt();
            }
        }

        // Clear conflict
        link.ConflictStatus      = null;
        link.ConflictOreoLeadsData = null;
        link.ConflictAirtableData  = null;
        link.ConflictResolvedAt  = DateTime.UtcNow;
        link.ConflictResolvedBy  = resolution.WinnerSource;
        link.SetUpdatedAt();

        await _db.SaveChangesAsync(ct);
    }

    // ── Main process ──────────────────────────────────────────────────────────

    public async Task ProcessJobAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _db.AirtableSyncJobs.FindAsync([jobId], ct);
        if (job is null)
        {
            _logger.LogWarning("AirtableSyncJob {JobId} not found.", jobId);
            return;
        }

        if (job.IsLocked)
        {
            _logger.LogWarning("AirtableSyncJob {JobId} is already locked — skipping.", jobId);
            return;
        }

        var config = await _db.AirtableConfigurations
                              .Include(c => c.FieldMappings)
                              .FirstOrDefaultAsync(c => c.Id == job.AirtableConfigurationId, ct);
        if (config is null)
        {
            await FailJobAsync(job, "Configuration not found.", ct);
            return;
        }

        var token = _configSvc.GetDecryptedAccessToken(config);
        if (string.IsNullOrWhiteSpace(token))
        {
            await FailJobAsync(job, "Access token missing or corrupted.", ct);
            return;
        }

        // Mark as processing
        job.Status       = AirtableSyncJobStatus.Processing;
        job.IsLocked     = true;
        job.StartedAt    = DateTime.UtcNow;
        job.AttemptCount++;
        job.LastAttemptAt = DateTime.UtcNow;
        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);

        try
        {
            switch (job.Direction)
            {
                case SyncDirection.OreoLeadsToAirtable:
                    await ExportLeadsAsync(job, config, token, ct);
                    break;

                case SyncDirection.AirtableToOreoLeads:
                    await ImportLeadsAsync(job, config, token, ct);
                    break;

                case SyncDirection.Bidirectional:
                    await ExportLeadsAsync(job, config, token, ct);
                    await ImportLeadsAsync(job, config, token, ct);
                    break;
            }

            // Update config LastSyncAt
            config.LastSyncAt = DateTime.UtcNow;
            config.SetUpdatedAt();

            job.Status      = AirtableSyncJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.IsLocked    = false;
            job.SetUpdatedAt();
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "AirtableSyncJob {JobId} completed. Success={Success} Failed={Failed} Conflicts={Conflicts}",
                job.Id, job.SuccessRecords, job.FailedRecords, job.ConflictRecords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AirtableSyncJob {JobId} failed.", jobId);
            var canRetry = job.AttemptCount < job.MaxAttempts;
            job.Status        = canRetry ? AirtableSyncJobStatus.Pending : AirtableSyncJobStatus.Failed;
            job.IsLocked      = false;
            job.ErrorMessage  = ex.Message;
            job.NextAttemptAt = canRetry ? DateTime.UtcNow.AddMinutes(5 * job.AttemptCount) : null;
            job.SetUpdatedAt();
            await _db.SaveChangesAsync(ct);
        }
    }

    // ── Export: OreoLeads → Airtable ──────────────────────────────────────────

    private async Task ExportLeadsAsync(
        AirtableSyncJob job, AirtableConfiguration config, string token,
        CancellationToken ct)
    {
        var mappings = config.FieldMappings.ToList();

        IQueryable<Lead> query = _db.Leads;

        if (job.LeadId.HasValue)
            query = query.Where(l => l.Id == job.LeadId.Value);
        else if (!job.IsFullSync && config.LastSyncAt.HasValue)
            query = query.Where(l => l.UpdatedAt >= config.LastSyncAt || l.CreatedAt >= config.LastSyncAt);

        var leads = await query.ToListAsync(ct);
        job.TotalRecords += leads.Count;
        await _db.SaveChangesAsync(ct);

        // Process in batches of 10
        for (var i = 0; i < leads.Count; i += 10)
        {
            if (ct.IsCancellationRequested) break;
            var batch = leads.Skip(i).Take(10).ToList();
            await ExportBatchAsync(job, config, token, mappings, batch, ct);
        }
    }

    private async Task ExportBatchAsync(
        AirtableSyncJob job, AirtableConfiguration config, string token,
        List<AirtableFieldMapping> mappings, List<Lead> batch,
        CancellationToken ct)
    {
        var toCreate = new List<(Lead lead, Dictionary<string, object?> fields)>();
        var toUpdate = new List<(Lead lead, AirtableRecordLink link, Dictionary<string, object?> fields)>();

        foreach (var lead in batch)
        {
            var link = await _db.AirtableRecordLinks
                                .FirstOrDefaultAsync(l =>
                                    l.LeadId == lead.Id &&
                                    l.AirtableConfigurationId == config.Id, ct);

            var fields = AirtableFieldMapper.MapLeadToAirtableFields(lead, mappings);
            var hash   = AirtableFieldMapper.ComputeHash(fields);

            if (link is null)
            {
                toCreate.Add((lead, fields));
            }
            else
            {
                // Check if data changed
                if (link.LastSyncHash == hash)
                {
                    AddLog(job, lead.Id, link.AirtableRecordId, "skipped", true, "No changes detected.");
                    job.ProcessedRecords++;
                    continue;
                }

                toUpdate.Add((lead, link, fields));
            }
        }

        // Batch create
        if (toCreate.Count > 0)
        {
            try
            {
                var fieldsList = toCreate.Select(x => x.fields).ToList();
                var ids        = await _airtable.CreateRecordsBatchAsync(
                    token, config.BaseId, config.TableIdOrName, fieldsList, ct);

                for (var j = 0; j < toCreate.Count; j++)
                {
                    var (lead, fields) = toCreate[j];
                    var airtableId     = j < ids.Count ? ids[j] : "";
                    var hash           = AirtableFieldMapper.ComputeHash(fields);

                    var link = new AirtableRecordLink
                    {
                        LeadId                  = lead.Id,
                        AirtableConfigurationId = config.Id,
                        OrganizationId          = config.OrganizationId,
                        AirtableRecordId        = airtableId,
                        LastSyncedAt            = DateTime.UtcNow,
                        LastSyncHash            = hash,
                    };
                    _db.AirtableRecordLinks.Add(link);

                    AddLog(job, lead.Id, airtableId, "created", true, null);
                    AddActivity(lead.Id, "Exported to Airtable (created)");
                    job.ProcessedRecords++;
                    job.SuccessRecords++;
                }
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                foreach (var (lead, _) in toCreate)
                {
                    AddLog(job, lead.Id, null, "error", false, ex.Message);
                    job.ProcessedRecords++;
                    job.FailedRecords++;
                }
            }
        }

        // Batch update
        if (toUpdate.Count > 0)
        {
            try
            {
                var updates = toUpdate.ToDictionary(
                    x => x.link.AirtableRecordId, x => x.fields);
                await _airtable.UpdateRecordsBatchAsync(
                    token, config.BaseId, config.TableIdOrName, updates, ct);

                foreach (var (lead, link, fields) in toUpdate)
                {
                    link.LastSyncedAt = DateTime.UtcNow;
                    link.LastSyncHash = AirtableFieldMapper.ComputeHash(fields);
                    link.SetUpdatedAt();

                    AddLog(job, lead.Id, link.AirtableRecordId, "updated", true, null);
                    AddActivity(lead.Id, "Exported to Airtable (updated)");
                    job.ProcessedRecords++;
                    job.SuccessRecords++;
                }
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                foreach (var (lead, link, _) in toUpdate)
                {
                    AddLog(job, lead.Id, link.AirtableRecordId, "error", false, ex.Message);
                    job.ProcessedRecords++;
                    job.FailedRecords++;
                }
            }
        }
    }

    // ── Import: Airtable → OreoLeads ──────────────────────────────────────────

    private async Task ImportLeadsAsync(
        AirtableSyncJob job, AirtableConfiguration config, string token,
        CancellationToken ct)
    {
        var mappings = config.FieldMappings.ToList();
        string? offset = null;

        string? filterFormula = null;
        if (!job.IsFullSync && config.LastSyncAt.HasValue)
        {
            var sinceStr = config.LastSyncAt.Value.ToString("yyyy-MM-ddTHH:mm:ss.000Z");
            filterFormula = $"IS_AFTER(LAST_MODIFIED_TIME(),'{sinceStr}')";
        }

        do
        {
            if (ct.IsCancellationRequested) break;

            var page = await _airtable.ListRecordsAsync(
                token, config.BaseId, config.TableIdOrName,
                offset, filterFormula, null, 100, ct);

            job.TotalRecords += page.Records.Count;

            foreach (var record in page.Records)
            {
                if (ct.IsCancellationRequested) break;
                await ImportRecordAsync(job, config, mappings, record, ct);
            }

            offset = page.Offset;
        }
        while (!string.IsNullOrEmpty(offset));
    }

    private async Task ImportRecordAsync(
        AirtableSyncJob job, AirtableConfiguration config,
        List<AirtableFieldMapping> mappings,
        AirtableRecordDto record, CancellationToken ct)
    {
        try
        {
            var link = await _db.AirtableRecordLinks
                                .Include(l => l.Lead)
                                .FirstOrDefaultAsync(l =>
                                    l.AirtableRecordId == record.Id &&
                                    l.AirtableConfigurationId == config.Id, ct);

            var fields = record.Fields;

            // Try to parse raw JSON strings from fields
            var parsedFields = new Dictionary<string, object?>();
            foreach (var kv in fields)
            {
                if (kv.Value is string s)
                {
                    // Strip surrounding quotes from JSON strings if present
                    var trimmed = s.Trim('"');
                    parsedFields[kv.Key] = trimmed;
                }
                else
                {
                    parsedFields[kv.Key] = kv.Value;
                }
            }

            if (link?.Lead is not null)
            {
                // Check for conflict when bidirectional
                if (job.Direction == SyncDirection.Bidirectional)
                {
                    var exportHash = AirtableFieldMapper.ComputeHash(
                        AirtableFieldMapper.MapLeadToAirtableFields(link.Lead, mappings));

                    if (link.LastSyncHash != null && link.LastSyncHash != exportHash)
                    {
                        // Both sides changed — conflict
                        await HandleConflictAsync(job, config, link, link.Lead, parsedFields, mappings, ct);
                        return;
                    }
                }

                // Apply import
                AirtableFieldMapper.MapAirtableFieldsToLead(parsedFields, mappings, link.Lead);
                link.Lead.SetUpdatedAt();
                link.LastSyncedAt = DateTime.UtcNow;
                link.LastSyncHash = AirtableFieldMapper.ComputeHash(
                    AirtableFieldMapper.MapLeadToAirtableFields(link.Lead, mappings));
                link.AirtableModifiedAt = record.ModifiedTime ?? record.CreatedTime;
                link.SetUpdatedAt();

                AddLog(job, link.Lead.Id, record.Id, "updated", true, null);
                AddActivity(link.Lead.Id, "Imported from Airtable (updated)");
                job.ProcessedRecords++;
                job.SuccessRecords++;
            }
            else
            {
                // Find by email
                string? email = null;
                var emailMapping = mappings.FirstOrDefault(m => m.OreoLeadsField == "Email");
                if (emailMapping is not null && parsedFields.TryGetValue(emailMapping.AirtableFieldName, out var eVal))
                    email = eVal?.ToString();

                Lead? lead = null;
                if (!string.IsNullOrWhiteSpace(email))
                    lead = await _db.Leads.FirstOrDefaultAsync(l => l.Email == email, ct);

                if (lead is null)
                {
                    lead = new Lead();
                    _db.Leads.Add(lead);
                }

                AirtableFieldMapper.MapAirtableFieldsToLead(parsedFields, mappings, lead);
                lead.SetUpdatedAt();

                if (link is null)
                {
                    link = new AirtableRecordLink
                    {
                        AirtableConfigurationId = config.Id,
                        OrganizationId          = config.OrganizationId,
                        AirtableRecordId        = record.Id,
                    };
                    _db.AirtableRecordLinks.Add(link);
                }

                link.LeadId         = lead.Id;
                link.LastSyncedAt   = DateTime.UtcNow;
                link.LastSyncHash   = AirtableFieldMapper.ComputeHash(
                    AirtableFieldMapper.MapLeadToAirtableFields(lead, mappings));
                link.AirtableModifiedAt = record.ModifiedTime ?? record.CreatedTime;
                link.SetUpdatedAt();

                AddLog(job, lead.Id, record.Id, "created", true, null);
                AddActivity(lead.Id, "Imported from Airtable (created)");
                job.ProcessedRecords++;
                job.SuccessRecords++;
            }

            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import Airtable record {RecordId}.", record.Id);
            AddLog(job, null, record.Id, "error", false, ex.Message);
            job.ProcessedRecords++;
            job.FailedRecords++;
        }
    }

    // ── Conflict handling ─────────────────────────────────────────────────────

    private async Task HandleConflictAsync(
        AirtableSyncJob job, AirtableConfiguration config,
        AirtableRecordLink link, Lead lead,
        Dictionary<string, object?> airtableFields,
        List<AirtableFieldMapping> mappings,
        CancellationToken ct)
    {
        var oreoLeadsData = JsonSerializer.Serialize(
            AirtableFieldMapper.MapLeadToAirtableFields(lead, mappings));
        var airtableData  = JsonSerializer.Serialize(airtableFields);

        switch (config.ConflictStrategy)
        {
            case ConflictStrategy.OreoLeadsWins:
                // Keep OreoLeads — just update hash
                link.LastSyncedAt = DateTime.UtcNow;
                link.SetUpdatedAt();
                AddLog(job, lead.Id, link.AirtableRecordId, "conflict", true, "OreoLeads wins.");
                job.ProcessedRecords++;
                job.SuccessRecords++;
                break;

            case ConflictStrategy.AirtableWins:
                AirtableFieldMapper.MapAirtableFieldsToLead(airtableFields, mappings, lead);
                lead.SetUpdatedAt();
                link.LastSyncedAt = DateTime.UtcNow;
                link.SetUpdatedAt();
                AddLog(job, lead.Id, link.AirtableRecordId, "conflict", true, "Airtable wins.");
                job.ProcessedRecords++;
                job.SuccessRecords++;
                break;

            case ConflictStrategy.MostRecentWins:
                var oreoLatest     = lead.UpdatedAt ?? lead.CreatedAt;
                var airtableLatest = link.AirtableModifiedAt ?? DateTime.MinValue;
                if (airtableLatest > oreoLatest)
                {
                    AirtableFieldMapper.MapAirtableFieldsToLead(airtableFields, mappings, lead);
                    lead.SetUpdatedAt();
                }
                link.LastSyncedAt = DateTime.UtcNow;
                link.SetUpdatedAt();
                AddLog(job, lead.Id, link.AirtableRecordId, "conflict", true, "Most recent wins.");
                job.ProcessedRecords++;
                job.SuccessRecords++;
                break;

            case ConflictStrategy.ManualResolution:
            default:
                link.ConflictStatus        = AirtableSyncJobStatus.Conflict;
                link.ConflictOreoLeadsData = oreoLeadsData;
                link.ConflictAirtableData  = airtableData;
                link.ConflictDetectedAt    = DateTime.UtcNow;
                link.SetUpdatedAt();
                AddLog(job, lead.Id, link.AirtableRecordId, "conflict", false, "Manual resolution required.");
                job.ProcessedRecords++;
                job.ConflictRecords++;
                break;
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task FailJobAsync(AirtableSyncJob job, string message, CancellationToken ct)
    {
        job.Status       = AirtableSyncJobStatus.Failed;
        job.IsLocked     = false;
        job.ErrorMessage = message;
        job.CompletedAt  = DateTime.UtcNow;
        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    private void AddLog(
        AirtableSyncJob job, Guid? leadId, string? airtableRecordId,
        string action, bool success, string? details)
    {
        _db.AirtableSyncLogs.Add(new AirtableSyncLog
        {
            AirtableSyncJobId = job.Id,
            OrganizationId    = job.OrganizationId,
            LeadId            = leadId,
            AirtableRecordId  = airtableRecordId,
            Action            = action,
            Success           = success,
            Details           = details,
            ErrorMessage      = success ? null : details,
            OccurredAt        = DateTime.UtcNow,
        });
    }

    private void AddActivity(Guid leadId, string description)
    {
        _db.Set<LeadActivity>().Add(new LeadActivity
        {
            LeadId      = leadId,
            Type        = ActivityType.Export,
            Description = description,
        });
    }
}
