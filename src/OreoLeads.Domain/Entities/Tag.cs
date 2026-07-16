using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1";

    // Navigation
    public ICollection<LeadTag> LeadTags { get; set; } = new List<LeadTag>();
}
