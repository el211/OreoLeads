using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

public class GeneratedEmail : BaseEntity
{
    public Guid LeadId { get; set; }

    // Content
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? CallToAction { get; set; }

    // Options
    public EmailStatus Status { get; set; } = EmailStatus.Generated;
    public EmailStyle Style { get; set; } = EmailStyle.Professional;
    public EmailLength Length { get; set; } = EmailLength.Normal;
    public EmailType Type { get; set; } = EmailType.FirstContact;

    // Provider metadata
    public string ProviderUsed { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public int GenerationMs { get; set; }
    public int CurrentVersion { get; set; } = 1;

    // Navigation
    public Lead Lead { get; set; } = null!;
    public ICollection<EmailDraftVersion> Versions { get; set; } = new List<EmailDraftVersion>();
}
