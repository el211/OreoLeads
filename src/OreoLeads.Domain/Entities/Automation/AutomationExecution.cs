using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities.Automation;

public class AutomationExecution : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public TriggerType TriggerType { get; set; }
    public string? TriggerData { get; set; }
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string? ContextJson { get; set; }
    public Guid? OrganizationId { get; set; }

    // Navigation
    public AutomationWorkflow? Workflow { get; set; }
    public ICollection<AutomationExecutionLog> Logs { get; set; } = new List<AutomationExecutionLog>();
    public ICollection<AutomationExecutionError> Errors { get; set; } = new List<AutomationExecutionError>();
}
