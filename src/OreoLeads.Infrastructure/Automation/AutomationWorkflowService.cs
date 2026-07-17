using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Entities.Automation;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Automation;

internal sealed class AutomationWorkflowService : IAutomationWorkflowService
{
    private readonly ApplicationDbContext _db;

    public AutomationWorkflowService(ApplicationDbContext db) => _db = db;

    // ── CRUD ─────────────────────────────────────────────────────────────────

    public async Task<List<WorkflowSummaryDto>> GetWorkflowsAsync(Guid? organizationId, CancellationToken ct = default)
    {
        var query = _db.AutomationWorkflows.AsQueryable();
        if (organizationId.HasValue)
            query = query.Where(w => w.OrganizationId == organizationId);

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WorkflowSummaryDto(
                w.Id, w.Name, w.Description, w.Status, w.IsEnabled,
                w.ExecutionCount, w.LastExecutedAt, w.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<AutomationWorkflow?> GetWorkflowAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.AutomationWorkflows
            .Include(w => w.Triggers)
            .Include(w => w.Actions.OrderBy(a => a.SortOrder))
            .Include(w => w.Conditions)
            .Include(w => w.Variables)
            .Include(w => w.Schedules)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task<AutomationWorkflow> CreateWorkflowAsync(CreateAutomationWorkflowDto dto, Guid? organizationId, CancellationToken ct = default)
    {
        var workflow = new AutomationWorkflow
        {
            Name = dto.Name,
            Description = dto.Description,
            FolderId = dto.FolderId,
            TriggerJson = dto.TriggerJson,
            ActionsJson = dto.ActionsJson,
            VariablesJson = dto.VariablesJson,
            Tags = dto.Tags,
            ConcurrencyLimit = dto.ConcurrencyLimit,
            TimeoutSeconds = dto.TimeoutSeconds,
            OrganizationId = organizationId,
            Status = WorkflowStatus.Draft
        };

        _db.AutomationWorkflows.Add(workflow);

        // Create initial version
        _db.AutomationVersions.Add(new AutomationVersion
        {
            WorkflowId = workflow.Id,
            VersionNumber = 1,
            Snapshot = JsonSerializer.Serialize(dto),
            Comment = "Initial version",
            OrganizationId = organizationId
        });

        await _db.SaveChangesAsync(ct);
        return workflow;
    }

    public async Task<AutomationWorkflow> UpdateWorkflowAsync(Guid id, UpdateAutomationWorkflowDto dto, CancellationToken ct = default)
    {
        var workflow = await _db.AutomationWorkflows.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Workflow {id} not found");

        workflow.Name = dto.Name;
        workflow.Description = dto.Description;
        workflow.FolderId = dto.FolderId;
        workflow.IsEnabled = dto.IsEnabled;
        workflow.TriggerJson = dto.TriggerJson;
        workflow.ActionsJson = dto.ActionsJson;
        workflow.VariablesJson = dto.VariablesJson;
        workflow.Tags = dto.Tags;
        workflow.ConcurrencyLimit = dto.ConcurrencyLimit;
        workflow.TimeoutSeconds = dto.TimeoutSeconds;
        workflow.Version++;
        workflow.SetUpdatedAt();

        // Save version
        _db.AutomationVersions.Add(new AutomationVersion
        {
            WorkflowId = workflow.Id,
            VersionNumber = workflow.Version,
            Snapshot = JsonSerializer.Serialize(dto),
            Comment = "Updated",
            OrganizationId = workflow.OrganizationId
        });

        await _db.SaveChangesAsync(ct);
        return workflow;
    }

    public async Task DeleteWorkflowAsync(Guid id, CancellationToken ct = default)
    {
        var workflow = await _db.AutomationWorkflows.FindAsync([id], ct);
        if (workflow is null) return;

        _db.AutomationWorkflows.Remove(workflow);
        await _db.SaveChangesAsync(ct);
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public async Task ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var workflow = await _db.AutomationWorkflows.FindAsync([id], ct);
        if (workflow is null) return;
        workflow.Status = WorkflowStatus.Active;
        workflow.IsEnabled = true;
        workflow.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var workflow = await _db.AutomationWorkflows.FindAsync([id], ct);
        if (workflow is null) return;
        workflow.Status = WorkflowStatus.Draft;
        workflow.IsEnabled = false;
        workflow.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task PauseAsync(Guid id, CancellationToken ct = default)
    {
        var workflow = await _db.AutomationWorkflows.FindAsync([id], ct);
        if (workflow is null) return;
        workflow.Status = WorkflowStatus.Paused;
        workflow.IsEnabled = false;
        workflow.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task ResumeAsync(Guid id, CancellationToken ct = default)
    {
        var workflow = await _db.AutomationWorkflows.FindAsync([id], ct);
        if (workflow is null) return;
        workflow.Status = WorkflowStatus.Active;
        workflow.IsEnabled = true;
        workflow.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    // ── Clone / Import / Export ──────────────────────────────────────────────

    public async Task<AutomationWorkflow> CloneWorkflowAsync(Guid id, CancellationToken ct = default)
    {
        var original = await GetWorkflowAsync(id, ct)
            ?? throw new InvalidOperationException($"Workflow {id} not found");

        var clone = new AutomationWorkflow
        {
            Name = $"{original.Name} (copie)",
            Description = original.Description,
            OrganizationId = original.OrganizationId,
            FolderId = original.FolderId,
            TriggerJson = original.TriggerJson,
            ActionsJson = original.ActionsJson,
            VariablesJson = original.VariablesJson,
            Tags = original.Tags,
            ConcurrencyLimit = original.ConcurrencyLimit,
            TimeoutSeconds = original.TimeoutSeconds,
            Status = WorkflowStatus.Draft
        };

        _db.AutomationWorkflows.Add(clone);
        await _db.SaveChangesAsync(ct);
        return clone;
    }

    public async Task<string> ExportWorkflowAsync(Guid id, CancellationToken ct = default)
    {
        var workflow = await GetWorkflowAsync(id, ct)
            ?? throw new InvalidOperationException($"Workflow {id} not found");

        var export = new
        {
            workflow.Name,
            workflow.Description,
            workflow.TriggerJson,
            workflow.ActionsJson,
            workflow.VariablesJson,
            workflow.Tags,
            workflow.ConcurrencyLimit,
            workflow.TimeoutSeconds
        };

        return JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<AutomationWorkflow> ImportWorkflowAsync(string json, Guid? organizationId, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var dto = new CreateAutomationWorkflowDto(
            Name: root.TryGetProperty("Name", out var n) ? n.GetString() ?? "Imported" : "Imported",
            Description: root.TryGetProperty("Description", out var d) ? d.GetString() : null,
            FolderId: null,
            TriggerJson: root.TryGetProperty("TriggerJson", out var t) ? t.GetString() : null,
            ActionsJson: root.TryGetProperty("ActionsJson", out var a) ? a.GetString() : null,
            VariablesJson: root.TryGetProperty("VariablesJson", out var v) ? v.GetString() : null,
            Tags: root.TryGetProperty("Tags", out var tg) ? tg.GetString() : null,
            ConcurrencyLimit: root.TryGetProperty("ConcurrencyLimit", out var cl) ? cl.GetInt32() : 1,
            TimeoutSeconds: root.TryGetProperty("TimeoutSeconds", out var ts) ? ts.GetInt32() : 300
        );

        return await CreateWorkflowAsync(dto, organizationId, ct);
    }

    // ── Versions ─────────────────────────────────────────────────────────────

    public async Task<List<AutomationVersion>> GetVersionsAsync(Guid workflowId, CancellationToken ct = default)
    {
        return await _db.AutomationVersions
            .Where(v => v.WorkflowId == workflowId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);
    }

    // ── Templates ────────────────────────────────────────────────────────────

    public async Task<List<AutomationTemplate>> GetTemplatesAsync(CancellationToken ct = default)
    {
        return await _db.AutomationTemplates
            .Where(t => t.IsBuiltIn || t.OrganizationId == null)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<AutomationWorkflow> UseTemplateAsync(Guid templateId, Guid? organizationId, CancellationToken ct = default)
    {
        var template = await _db.AutomationTemplates.FindAsync([templateId], ct)
            ?? throw new InvalidOperationException($"Template {templateId} not found");

        var dto = new CreateAutomationWorkflowDto(
            Name: template.Name,
            Description: template.Description,
            FolderId: null,
            TriggerJson: template.TriggerJson,
            ActionsJson: template.ActionsJson,
            VariablesJson: template.VariablesJson,
            Tags: template.Tags
        );

        return await CreateWorkflowAsync(dto, organizationId, ct);
    }

    public async Task SeedBuiltInTemplatesAsync(CancellationToken ct = default)
    {
        if (await _db.AutomationTemplates.AnyAsync(t => t.IsBuiltIn, ct))
            return;

        var templates = new List<AutomationTemplate>
        {
            new()
            {
                Name = "Bienvenue",
                Description = "Envoie un email de bienvenue quand un lead est créé",
                Category = "Onboarding",
                IsBuiltIn = true,
                IconName = "Mail",
                TriggerJson = JsonSerializer.Serialize(new { type = "LeadCreated" }),
                ActionsJson = JsonSerializer.Serialize(new[] { new { type = "SendEmail", name = "Email bienvenue", config = new { subject = "Bienvenue !", body = "Bienvenue dans notre CRM." } } })
            },
            new()
            {
                Name = "Prospection",
                Description = "Séquence de prospection automatique pour les nouveaux prospects",
                Category = "Sales",
                IsBuiltIn = true,
                IconName = "Target",
                TriggerJson = JsonSerializer.Serialize(new { type = "LeadCreated" }),
                ActionsJson = JsonSerializer.Serialize(new object[]
                {
                    new { type = "Wait", name = "Attente 1j", config = new { days = 1 } },
                    new { type = "SendEmail", name = "Email intro", config = new { subject = "Introduction", body = "Premier contact." } },
                    new { type = "Wait", name = "Attente 3j", config = new { days = 3 } },
                    new { type = "SendEmail", name = "Email relance", config = new { subject = "Suite à notre premier contact", body = "Relance." } }
                })
            },
            new()
            {
                Name = "Relance",
                Description = "Envoie un email et crée une note quand une relance est due",
                Category = "Follow-up",
                IsBuiltIn = true,
                IconName = "Bell",
                TriggerJson = JsonSerializer.Serialize(new { type = "FollowUpDue" }),
                ActionsJson = JsonSerializer.Serialize(new object[]
                {
                    new { type = "SendEmail", name = "Email relance", config = new { subject = "Relance", body = "N'oubliez pas notre rendez-vous." } },
                    new { type = "CreateNote", name = "Note relance", config = new { content = "Relance automatique envoyée." } }
                })
            },
            new()
            {
                Name = "Lead froid",
                Description = "Réengage les leads sans contact depuis 30 jours",
                Category = "Re-engagement",
                IsBuiltIn = true,
                IconName = "Snowflake",
                TriggerJson = JsonSerializer.Serialize(new { type = "Cron", config = new { interval = "Monthly" } }),
                ActionsJson = JsonSerializer.Serialize(new[] { new { type = "SendEmail", name = "Email réengagement", config = new { subject = "De vos nouvelles ?", body = "Cela fait un moment..." } } })
            },
            new()
            {
                Name = "Lead chaud",
                Description = "Crée un suivi et envoie un email quand un lead passe en Hot",
                Category = "Sales",
                IsBuiltIn = true,
                IconName = "Flame",
                TriggerJson = JsonSerializer.Serialize(new { type = "StatusChanged", config = new { status = "Hot" } }),
                ActionsJson = JsonSerializer.Serialize(new object[]
                {
                    new { type = "CreateFollowUp", name = "Suivi urgent", config = new { comment = "Lead chaud, contacter rapidement", daysDelay = 1 } },
                    new { type = "SendEmail", name = "Email prioritaire", config = new { subject = "Opportunité détectée", body = "Un lead chaud nécessite votre attention." } }
                })
            },
            new()
            {
                Name = "Après ouverture email",
                Description = "Change le statut après ouverture d'un email",
                Category = "Email",
                IsBuiltIn = true,
                IconName = "Eye",
                TriggerJson = JsonSerializer.Serialize(new { type = "EmailOpened" }),
                ActionsJson = JsonSerializer.Serialize(new object[]
                {
                    new { type = "Wait", name = "Attente 2h", config = new { hours = 2 } },
                    new { type = "ChangeStatus", name = "Passer en Qualified", config = new { status = "Qualified" } }
                })
            },
            new()
            {
                Name = "Après clic email",
                Description = "Ajoute un tag et crée un suivi après un clic dans un email",
                Category = "Email",
                IsBuiltIn = true,
                IconName = "MousePointerClick",
                TriggerJson = JsonSerializer.Serialize(new { type = "EmailClicked" }),
                ActionsJson = JsonSerializer.Serialize(new object[]
                {
                    new { type = "AddTag", name = "Tag engagé", config = new { tag = "Engaged" } },
                    new { type = "CreateFollowUp", name = "Suivi clic", config = new { comment = "Le prospect a cliqué sur un lien", daysDelay = 1 } }
                })
            },
            new()
            {
                Name = "Après réponse email",
                Description = "Qualifie le lead et crée une note après une réponse email",
                Category = "Email",
                IsBuiltIn = true,
                IconName = "Reply",
                TriggerJson = JsonSerializer.Serialize(new { type = "EmailReplied" }),
                ActionsJson = JsonSerializer.Serialize(new object[]
                {
                    new { type = "ChangeStatus", name = "Qualifier", config = new { status = "Qualified" } },
                    new { type = "CreateNote", name = "Note réponse", config = new { content = "Le prospect a répondu à un email." } }
                })
            }
        };

        _db.AutomationTemplates.AddRange(templates);
        await _db.SaveChangesAsync(ct);
    }

    // ── Executions ───────────────────────────────────────────────────────────

    public async Task<List<ExecutionSummaryDto>> GetExecutionsAsync(Guid? workflowId, Guid? organizationId, CancellationToken ct = default)
    {
        var query = _db.AutomationExecutions.AsQueryable();
        if (workflowId.HasValue)
            query = query.Where(e => e.WorkflowId == workflowId);
        if (organizationId.HasValue)
            query = query.Where(e => e.OrganizationId == organizationId);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(200)
            .Select(e => new ExecutionSummaryDto(
                e.Id, e.WorkflowId, e.WorkflowName, e.TriggerType,
                e.Status, e.StartedAt, e.CompletedAt, e.DurationMs, e.RetryCount))
            .ToListAsync(ct);
    }

    public async Task<AutomationExecution?> GetExecutionAsync(Guid executionId, CancellationToken ct = default)
    {
        return await _db.AutomationExecutions
            .Include(e => e.Logs.OrderBy(l => l.Timestamp))
            .Include(e => e.Errors)
            .FirstOrDefaultAsync(e => e.Id == executionId, ct);
    }

    public async Task<List<AutomationExecutionLog>> GetExecutionLogsAsync(Guid executionId, CancellationToken ct = default)
    {
        return await _db.AutomationExecutionLogs
            .Where(l => l.ExecutionId == executionId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync(ct);
    }

    // ── Monitoring ───────────────────────────────────────────────────────────

    public async Task<MonitoringStatsDto> GetMonitoringStatsAsync(Guid? organizationId, CancellationToken ct = default)
    {
        var workflowQuery = _db.AutomationWorkflows.AsQueryable();
        var executionQuery = _db.AutomationExecutions.AsQueryable();
        var queueQuery = _db.AutomationQueueItems.AsQueryable();

        if (organizationId.HasValue)
        {
            workflowQuery = workflowQuery.Where(w => w.OrganizationId == organizationId);
            executionQuery = executionQuery.Where(e => e.OrganizationId == organizationId);
            queueQuery = queueQuery.Where(q => q.OrganizationId == organizationId);
        }

        var totalWorkflows = await workflowQuery.CountAsync(ct);
        var activeWorkflows = await workflowQuery.CountAsync(w => w.IsEnabled && w.Status == WorkflowStatus.Active, ct);
        var totalExecutions = await executionQuery.CountAsync(ct);
        var successfulExecutions = await executionQuery.CountAsync(e => e.Status == ExecutionStatus.Completed, ct);
        var failedExecutions = await executionQuery.CountAsync(e => e.Status == ExecutionStatus.Failed, ct);
        var avgDuration = totalExecutions > 0
            ? await executionQuery.Where(e => e.DurationMs.HasValue).AverageAsync(e => (double)e.DurationMs!.Value, ct)
            : 0;
        var queueDepth = await queueQuery.CountAsync(q => q.Status == QueueItemStatus.Pending || q.Status == QueueItemStatus.Retrying, ct);
        var activeJobs = await queueQuery.CountAsync(q => q.Status == QueueItemStatus.Running, ct);
        var failedJobs = await queueQuery.CountAsync(q => q.Status == QueueItemStatus.Failed, ct);
        var deadLetter = await queueQuery.CountAsync(q => q.Status == QueueItemStatus.DeadLetter, ct);

        var successRate = totalExecutions > 0 ? (double)successfulExecutions / totalExecutions * 100 : 0;

        return new MonitoringStatsDto(
            totalWorkflows, activeWorkflows, totalExecutions,
            successfulExecutions, failedExecutions, successRate,
            avgDuration, queueDepth, activeJobs, failedJobs, deadLetter);
    }
}
