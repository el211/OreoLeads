using OreoLeads.Domain.Enums;

namespace OreoLeads.Application.Features.LeadActivities.DTOs;

public class LeadActivityDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public ActivityType Type { get; init; }
    public string TypeLabel { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? UserId { get; init; }
    public string? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }
}
