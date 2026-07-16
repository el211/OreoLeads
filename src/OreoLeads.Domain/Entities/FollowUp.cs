using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

public class FollowUp : BaseEntity
{
    public Guid LeadId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Pending;
    public LeadPriority Priority { get; set; } = LeadPriority.Medium;
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public Lead Lead { get; set; } = null!;
}
