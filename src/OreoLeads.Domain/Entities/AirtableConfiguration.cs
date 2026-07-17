using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

public class AirtableConfiguration : BaseEntity
{
    public Guid? OrganizationId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public string? EncryptedAccessToken { get; set; }
    public string BaseId { get; set; } = string.Empty;
    public string TableIdOrName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = false;
    public SyncDirection SyncDirection { get; set; } = SyncDirection.OreoLeadsToAirtable;
    public ConflictStrategy ConflictStrategy { get; set; } = ConflictStrategy.OreoLeadsWins;
    public DateTime? LastSyncAt { get; set; }
    public string? WebhookId { get; set; }
    public string? WebhookCursor { get; set; }
    public DateTime? WebhookExpiresAt { get; set; }
    public ICollection<AirtableFieldMapping> FieldMappings { get; set; } = new List<AirtableFieldMapping>();
}
