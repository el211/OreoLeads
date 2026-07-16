using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

public class LeadActivity : BaseEntity
{
    public Guid LeadId { get; set; }
    public ActivityType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? Metadata { get; set; }

    // Navigation
    public Lead Lead { get; set; } = null!;
}
