using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities.Automation;

public class AutomationFolder : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }

    // Navigation
    public ICollection<AutomationWorkflow> Workflows { get; set; } = new List<AutomationWorkflow>();
}
