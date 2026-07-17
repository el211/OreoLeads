using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Entities.Automation;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Automation;

internal sealed class AutomationEngine : IAutomationEngine
{
    private readonly ApplicationDbContext _db;
    private readonly IAutomationExecutor _executor;
    private readonly IAutomationValidator _validator;
    private readonly ILogger<AutomationEngine> _logger;

    public AutomationEngine(
        ApplicationDbContext db,
        IAutomationExecutor executor,
        IAutomationValidator validator,
        ILogger<AutomationEngine> logger)
    {
        _db = db;
        _executor = executor;
        _validator = validator;
        _logger = logger;
    }

    public async Task<AutomationExecutionResultDto> TriggerAsync(TriggerEventDto triggerEvent, CancellationToken ct = default)
    {
        // Find all active workflows with matching trigger type
        var workflows = await _db.AutomationWorkflows
            .Where(w => w.IsEnabled && w.Status == WorkflowStatus.Active)
            .Where(w => triggerEvent.OrganizationId == null || w.OrganizationId == triggerEvent.OrganizationId)
            .Include(w => w.Triggers)
            .ToListAsync(ct);

        var matchingWorkflows = workflows
            .Where(w => w.Triggers.Any(t => t.Type == triggerEvent.Type))
            .ToList();

        if (matchingWorkflows.Count == 0)
            return new AutomationExecutionResultDto(true, null, "No matching workflows", new List<string>());

        var errors = new List<string>();
        Guid? lastExecutionId = null;

        foreach (var workflow in matchingWorkflows)
        {
            // Check max executions
            if (workflow.MaxExecutions.HasValue && workflow.ExecutionCount >= workflow.MaxExecutions.Value)
            {
                _logger.LogDebug("Workflow {Id} reached max executions ({Max})", workflow.Id, workflow.MaxExecutions);
                continue;
            }

            var result = await ExecuteWorkflowAsync(workflow.Id, triggerEvent, ct);
            if (!result.Success) errors.AddRange(result.Errors);
            lastExecutionId = result.ExecutionId ?? lastExecutionId;
        }

        return new AutomationExecutionResultDto(
            errors.Count == 0,
            lastExecutionId,
            errors.Count == 0 ? $"Triggered {matchingWorkflows.Count} workflow(s)" : "Some workflows failed",
            errors);
    }

    public async Task<AutomationExecutionResultDto> ExecuteWorkflowAsync(Guid workflowId, TriggerEventDto triggerEvent, CancellationToken ct = default)
    {
        var workflow = await _db.AutomationWorkflows
            .Include(w => w.Actions.OrderBy(a => a.SortOrder))
            .FirstOrDefaultAsync(w => w.Id == workflowId, ct);

        if (workflow is null)
            return new AutomationExecutionResultDto(false, null, $"Workflow {workflowId} not found", ["Workflow not found"]);

        if (!workflow.IsEnabled)
            return new AutomationExecutionResultDto(false, null, "Workflow is disabled", ["Workflow is disabled"]);

        // Check max executions
        if (workflow.MaxExecutions.HasValue && workflow.ExecutionCount >= workflow.MaxExecutions.Value)
            return new AutomationExecutionResultDto(false, null, "Max executions reached", ["Max executions reached"]);

        // Create execution record
        var execution = new AutomationExecution
        {
            WorkflowId = workflow.Id,
            WorkflowName = workflow.Name,
            TriggerType = triggerEvent.Type,
            TriggerData = JsonSerializer.Serialize(triggerEvent.Data),
            Status = ExecutionStatus.Running,
            StartedAt = DateTime.UtcNow,
            OrganizationId = workflow.OrganizationId
        };

        _db.AutomationExecutions.Add(execution);
        await _db.SaveChangesAsync(ct);

        var context = new AutomationContext
        {
            WorkflowId = workflow.Id,
            ExecutionId = execution.Id,
            OrganizationId = workflow.OrganizationId,
            LeadId = triggerEvent.LeadId,
            TriggerData = triggerEvent.Data ?? new Dictionary<string, object?>()
        };

        var sw = Stopwatch.StartNew();
        var errors = new List<string>();

        try
        {
            foreach (var action in workflow.Actions)
            {
                ct.ThrowIfCancellationRequested();

                // Timeout check
                if (sw.ElapsedMilliseconds > (long)workflow.TimeoutSeconds * 1000)
                {
                    execution.Status = ExecutionStatus.TimedOut;
                    errors.Add("Execution timed out");
                    break;
                }

                // Evaluate conditions for this action
                if (!string.IsNullOrWhiteSpace(action.ConditionsJson) &&
                    !_validator.EvaluateConditions(action.ConditionsJson, context))
                {
                    LogAction(execution.Id, action, "Conditions not met, skipping");
                    continue;
                }

                try
                {
                    var result = await _executor.ExecuteActionAsync(action, context, ct);

                    LogAction(execution.Id, action, result.Success ? $"Success: {result.Output}" : $"Failed: {result.Error}");

                    if (!result.Success)
                    {
                        errors.Add($"Action '{action.Name}' failed: {result.Error}");

                        if (!action.ContinueOnError)
                        {
                            execution.Status = ExecutionStatus.Failed;
                            execution.ErrorMessage = result.Error;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Action '{action.Name}' threw: {ex.Message}");
                    LogError(execution.Id, action, ex);

                    if (!action.ContinueOnError)
                    {
                        execution.Status = ExecutionStatus.Failed;
                        execution.ErrorMessage = ex.Message;
                        break;
                    }
                }
            }

            if (execution.Status == ExecutionStatus.Running)
                execution.Status = ExecutionStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            execution.Status = ExecutionStatus.Cancelled;
            errors.Add("Execution was cancelled");
        }
        catch (Exception ex)
        {
            execution.Status = ExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;
            errors.Add(ex.Message);
        }

        sw.Stop();
        execution.DurationMs = sw.ElapsedMilliseconds;
        execution.CompletedAt = DateTime.UtcNow;
        execution.SetUpdatedAt();

        workflow.ExecutionCount++;
        workflow.LastExecutedAt = DateTime.UtcNow;
        workflow.SetUpdatedAt();

        await _db.SaveChangesAsync(ct);

        return new AutomationExecutionResultDto(
            execution.Status == ExecutionStatus.Completed,
            execution.Id,
            execution.Status == ExecutionStatus.Completed ? "Execution completed" : execution.ErrorMessage ?? "Execution failed",
            errors);
    }

    public async Task CancelExecutionAsync(Guid executionId, CancellationToken ct = default)
    {
        var execution = await _db.AutomationExecutions.FindAsync([executionId], ct);
        if (execution is null) return;

        if (execution.Status is ExecutionStatus.Running or ExecutionStatus.Pending or ExecutionStatus.Waiting)
        {
            execution.Status = ExecutionStatus.Cancelled;
            execution.CompletedAt = DateTime.UtcNow;
            execution.SetUpdatedAt();
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task RetryExecutionAsync(Guid executionId, CancellationToken ct = default)
    {
        var execution = await _db.AutomationExecutions.FindAsync([executionId], ct);
        if (execution is null || execution.Status != ExecutionStatus.Failed) return;

        execution.RetryCount++;
        execution.Status = ExecutionStatus.Pending;
        execution.ErrorMessage = null;
        execution.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);

        var triggerData = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(execution.TriggerData))
        {
            try
            {
                triggerData = JsonSerializer.Deserialize<Dictionary<string, object?>>(execution.TriggerData) ?? triggerData;
            }
            catch { /* use empty */ }
        }

        var triggerEvent = new TriggerEventDto(execution.TriggerType, null, execution.OrganizationId, triggerData);
        await ExecuteWorkflowAsync(execution.WorkflowId, triggerEvent, ct);
    }

    private void LogAction(Guid executionId, AutomationAction action, string message)
    {
        _db.AutomationExecutionLogs.Add(new AutomationExecutionLog
        {
            ExecutionId = executionId,
            ActionId = action.Id,
            ActionName = action.Name,
            Message = message,
            Level = "Info",
            Timestamp = DateTime.UtcNow,
            OrganizationId = action.OrganizationId
        });
    }

    private void LogError(Guid executionId, AutomationAction action, Exception ex)
    {
        _db.AutomationExecutionErrors.Add(new AutomationExecutionError
        {
            ExecutionId = executionId,
            ActionId = action.Id,
            ActionName = action.Name,
            ErrorType = ex.GetType().Name,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            OccurredAt = DateTime.UtcNow,
            IsRetryable = true,
            OrganizationId = action.OrganizationId
        });
    }
}
