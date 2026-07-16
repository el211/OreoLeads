using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

public class EmailDraftVersion : BaseEntity
{
    public Guid EmailDraftId { get; set; }
    public int Version { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string ProviderUsed { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public int GenerationMs { get; set; }

    // Navigation
    public GeneratedEmail EmailDraft { get; set; } = null!;
}
