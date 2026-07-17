using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

public class AirtableSyncJob : BaseEntity
{
    public Guid AirtableConfigurationId { get; set; }
    public Guid? OrganizationId { get; set; }
    public AirtableSyncJobStatus Status { get; set; } = AirtableSyncJobStatus.Pending;
    public SyncDirection Direction { get; set; }
    public string? TriggerReason { get; set; }   // "manual", "scheduled", "webhook"
    public bool IsFullSync { get; set; } = false;
    public Guid? LeadId { get; set; }             // null = all leads
    public string? LeadFilter { get; set; }       // JSON filter
    public int TotalRecords { get; set; } = 0;
    public int ProcessedRecords { get; set; } = 0;
    public int SuccessRecords { get; set; } = 0;
    public int FailedRecords { get; set; } = 0;
    public int ConflictRecords { get; set; } = 0;
    public int AttemptCount { get; set; } = 0;
    public int MaxAttempts { get; set; } = 3;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsLocked { get; set; } = false;   // prevent double processing
    public string? AirtableOffset { get; set; }   // pagination cursor
    public ICollection<AirtableSyncLog> Logs { get; set; } = new List<AirtableSyncLog>();
}
