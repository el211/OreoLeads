using OreoLeads.Domain.Enums;

namespace OreoLeads.Application.Common.Interfaces;

public record AiCompletionRequest(
    string SystemPrompt,
    string UserPrompt,
    int MaxTokens = 1000,
    float Temperature = 0.7f);

public record AiCompletionResult(
    string Content,
    string Model,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    int GenerationMs);

public interface IAiProvider
{
    string ProviderName { get; }
    AiProviderType ProviderType { get; }
    bool IsConfigured { get; }

    Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default);
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}
