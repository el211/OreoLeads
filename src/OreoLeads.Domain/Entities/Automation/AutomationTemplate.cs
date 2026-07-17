using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities.Automation;

public class AutomationTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? TriggerJson { get; set; }
    public string? ActionsJson { get; set; }
    public string? VariablesJson { get; set; }
    public bool IsBuiltIn { get; set; }
    public string? IconName { get; set; }
    public string? Tags { get; set; }
    public Guid? OrganizationId { get; set; }
}
