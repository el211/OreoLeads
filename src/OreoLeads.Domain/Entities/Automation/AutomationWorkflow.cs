using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities.Automation;

public class AutomationWorkflow : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? FolderId { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;
    public bool IsEnabled { get; set; }
    public int Version { get; set; } = 1;
    public int? MaxExecutions { get; set; }
    public int ConcurrencyLimit { get; set; } = 1;
    public int TimeoutSeconds { get; set; } = 300;
    public string? Tags { get; set; }
    public string? TriggerJson { get; set; }
    public string? ActionsJson { get; set; }
    public string? VariablesJson { get; set; }
    public int ExecutionCount { get; set; }
    public DateTime? LastExecutedAt { get; set; }

    // Navigation
    public AutomationFolder? Folder { get; set; }
    public ICollection<AutomationTrigger> Triggers { get; set; } = new List<AutomationTrigger>();
    public ICollection<AutomationAction> Actions { get; set; } = new List<AutomationAction>();
    public ICollection<AutomationCondition> Conditions { get; set; } = new List<AutomationCondition>();
    public ICollection<AutomationVariable> Variables { get; set; } = new List<AutomationVariable>();
    public ICollection<AutomationSchedule> Schedules { get; set; } = new List<AutomationSchedule>();
    public ICollection<AutomationExecution> Executions { get; set; } = new List<AutomationExecution>();
    public ICollection<AutomationVersion> Versions { get; set; } = new List<AutomationVersion>();
}
