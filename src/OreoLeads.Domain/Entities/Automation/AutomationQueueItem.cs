using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities.Automation;

public class AutomationQueueItem : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public Guid? ExecutionId { get; set; }
    public QueueItemStatus Status { get; set; } = QueueItemStatus.Pending;
    public int Priority { get; set; }
    public string? Payload { get; set; }
    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public DateTime? NextRetryAt { get; set; }
    public string? LockedBy { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? OrganizationId { get; set; }
}
