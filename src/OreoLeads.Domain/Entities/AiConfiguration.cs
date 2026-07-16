using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

/// <summary>Single-row table: stores the active AI provider configuration.</summary>
public class AiConfiguration : BaseEntity
{
    public AiProviderType ProviderType { get; set; } = AiProviderType.Claude;
    public string? EncryptedApiKey { get; set; }
    public string? Model { get; set; }
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 1000;
    public int TimeoutSeconds { get; set; } = 30;
    public bool IsEnabled { get; set; } = false;
    public string? BaseUrl { get; set; }
}
