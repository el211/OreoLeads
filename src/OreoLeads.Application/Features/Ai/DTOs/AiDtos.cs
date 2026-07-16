using OreoLeads.Domain.Enums;

namespace OreoLeads.Application.Features.Ai.DTOs;

// ── Provider info ─────────────────────────────────────────────────────────────

public record AiProviderInfoDto(
    string Name,
    AiProviderType Type,
    bool RequiresApiKey,
    bool RequiresBaseUrl,
    string[] DefaultModels,
    string Description);

// ── Configuration ─────────────────────────────────────────────────────────────

public class AiConfigurationDto
{
    public Guid? Id { get; init; }
    public AiProviderType ProviderType { get; init; }
    public bool HasApiKey { get; init; }
    public string? Model { get; init; }
    public float Temperature { get; init; }
    public int MaxTokens { get; init; }
    public int TimeoutSeconds { get; init; }
    public bool IsEnabled { get; init; }
    public string? BaseUrl { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class UpdateAiConfigurationDto
{
    public AiProviderType ProviderType { get; init; } = AiProviderType.Claude;
    public string? ApiKey { get; init; }           // null = don't change
    public string? Model { get; init; }
    public float Temperature { get; init; } = 0.7f;
    public int MaxTokens { get; init; } = 1000;
    public int TimeoutSeconds { get; init; } = 30;
    public bool IsEnabled { get; init; } = false;
    public string? BaseUrl { get; init; }
}

// ── Test connection ───────────────────────────────────────────────────────────

public record AiTestResultDto(
    bool Success,
    string Message,
    int LatencyMs,
    string? Model);

// ── Email generation ──────────────────────────────────────────────────────────

public class GenerateEmailRequestDto
{
    public EmailStyle Style { get; init; } = EmailStyle.Professional;
    public EmailLength Length { get; init; } = EmailLength.Normal;
    public EmailType Type { get; init; } = EmailType.FirstContact;
    public string? CustomInstructions { get; init; }
}

// ── Prompt template ───────────────────────────────────────────────────────────

public class PromptTemplateDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Description { get; init; }
    public EmailType? EmailType { get; init; }
    public bool IsSystem { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class UpdatePromptTemplateDto
{
    public string Content { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? Description { get; init; }
}

// ── Stats ─────────────────────────────────────────────────────────────────────

public class AiStatsDto
{
    public int TotalGenerations { get; init; }
    public int TotalTokens { get; init; }
    public double AverageGenerationMs { get; init; }
    public Dictionary<string, int> GenerationsByProvider { get; init; } = new();
}
