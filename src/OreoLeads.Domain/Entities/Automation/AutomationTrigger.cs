using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities.Automation;

public class AutomationTrigger : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public TriggerType Type { get; set; }
    public string? ConfigJson { get; set; }
    public Guid? OrganizationId { get; set; }

    // Navigation
    public AutomationWorkflow? Workflow { get; set; }
}
