using OreoLeads.Domain.Enums;

namespace OreoLeads.Application.Features.Ai.DTOs;

public class EmailDraftDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string CompanyName { get; init; } = string.Empty;

    // Content
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? CallToAction { get; init; }

    // Options
    public EmailStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public EmailStyle Style { get; init; }
    public EmailLength Length { get; init; }
    public EmailType Type { get; init; }
    public string TypeLabel { get; init; } = string.Empty;

    // Provider
    public string ProviderUsed { get; init; } = string.Empty;
    public string ModelUsed { get; init; } = string.Empty;
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens { get; init; }
    public int GenerationMs { get; init; }
    public int CurrentVersion { get; init; }

    // Lead info (for display)
    public int? BusinessScore { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class EmailDraftVersionDto
{
    public Guid Id { get; init; }
    public Guid EmailDraftId { get; init; }
    public int Version { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string ProviderUsed { get; init; } = string.Empty;
    public string ModelUsed { get; init; } = string.Empty;
    public int GenerationMs { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class UpdateEmailDraftDto
{
    public string? Subject { get; init; }
    public string? Body { get; init; }
}
