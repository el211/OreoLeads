using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities.Automation;

public class AutomationVariable : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string Type { get; set; } = "string";
    public bool IsGlobal { get; set; }
    public Guid? OrganizationId { get; set; }

    // Navigation
    public AutomationWorkflow? Workflow { get; set; }
}
