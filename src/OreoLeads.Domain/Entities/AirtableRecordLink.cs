using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

public class AirtableRecordLink : BaseEntity
{
    public Guid LeadId { get; set; }
    public Lead? Lead { get; set; }
    public Guid AirtableConfigurationId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string AirtableRecordId { get; set; } = string.Empty;
    public DateTime? LastSyncedAt { get; set; }
    public string? LastSyncHash { get; set; }            // hash of last synced data
    public AirtableSyncJobStatus? ConflictStatus { get; set; }
    public string? ConflictOreoLeadsData { get; set; }  // JSON
    public string? ConflictAirtableData { get; set; }   // JSON
    public DateTime? ConflictDetectedAt { get; set; }
    public DateTime? ConflictResolvedAt { get; set; }
    public string? ConflictResolvedBy { get; set; }
    public DateTime? AirtableModifiedAt { get; set; }
}
