using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities.Automation;

public class AutomationVersion : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public int VersionNumber { get; set; }
    public string Snapshot { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string? CreatedBy { get; set; }
    public Guid? OrganizationId { get; set; }

    // Navigation
    public AutomationWorkflow? Workflow { get; set; }
}
