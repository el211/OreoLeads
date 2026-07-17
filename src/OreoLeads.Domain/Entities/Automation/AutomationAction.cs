using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities.Automation;

public class AutomationAction : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ActionType Type { get; set; }
    public string? ConfigJson { get; set; }
    public string? ConditionsJson { get; set; }
    public int SortOrder { get; set; }
    public bool ContinueOnError { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public Guid? OrganizationId { get; set; }

    // Navigation
    public AutomationWorkflow? Workflow { get; set; }
}
