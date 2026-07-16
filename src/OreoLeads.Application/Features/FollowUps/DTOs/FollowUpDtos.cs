using OreoLeads.Domain.Enums;

namespace OreoLeads.Application.Features.FollowUps.DTOs;

public class FollowUpDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string? CompanyName { get; init; }
    public DateTime ScheduledAt { get; init; }
    public Guid? UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public FollowUpStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public LeadPriority Priority { get; init; }
    public string PriorityLabel { get; init; } = string.Empty;
    public DateTime? CompletedAt { get; init; }
    public bool IsOverdue => Status == FollowUpStatus.Pending && ScheduledAt < DateTime.UtcNow;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class CreateFollowUpDto
{
    public DateTime ScheduledAt { get; init; }
    public string UserName { get; init; } = "Système";
    public string? Comment { get; init; }
    public LeadPriority Priority { get; init; } = LeadPriority.Medium;
}

public class UpdateFollowUpDto
{
    public DateTime ScheduledAt { get; init; }
    public string? Comment { get; init; }
    public FollowUpStatus Status { get; init; }
    public LeadPriority Priority { get; init; }
    public DateTime? CompletedAt { get; init; }
}
