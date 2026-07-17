using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

public class AirtableSyncLog : BaseEntity
{
    public Guid AirtableSyncJobId { get; set; }
    public AirtableSyncJob? AirtableSyncJob { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? LeadId { get; set; }
    public string? AirtableRecordId { get; set; }
    public string Action { get; set; } = string.Empty;  // "created", "updated", "skipped", "conflict", "error"
    public string? Details { get; set; }                 // JSON
    public string? ErrorMessage { get; set; }
    public bool Success { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
