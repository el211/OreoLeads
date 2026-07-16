using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

/// <summary>Modifiable prompt template. All prompts are stored here, never hardcoded.</summary>
public class PromptTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique identifier used by the code (e.g. "email.system", "email.first_contact").</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Prompt content with optional {{placeholder}} variables.</summary>
    public string Content { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>If set, this template is specific to an email type.</summary>
    public EmailType? EmailType { get; set; }

    /// <summary>System templates cannot be deleted, only edited.</summary>
    public bool IsSystem { get; set; } = true;
}
