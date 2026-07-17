using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities.Automation;

public class AutomationCondition : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public string GroupId { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public ConditionOperator Operator { get; set; }
    public string? Value { get; set; }
    public LogicOperator LogicOperator { get; set; }
    public int SortOrder { get; set; }
    public Guid? OrganizationId { get; set; }

    // Navigation
    public AutomationWorkflow? Workflow { get; set; }
}
