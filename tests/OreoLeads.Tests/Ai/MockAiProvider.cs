using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Tests.Ai;

/// <summary>Mock provider that returns a predictable JSON response without any HTTP call.</summary>
internal sealed class MockAiProvider : IAiProvider
{
    public string ProviderName => "MockProvider";
    public AiProviderType ProviderType => AiProviderType.Claude;
    public bool IsConfigured => true;

    private readonly string _responseOverride;
    private readonly bool _testResult;

    public MockAiProvider(string? responseOverride = null, bool testResult = true)
    {
        _responseOverride = responseOverride ?? """
        {
          "subject": "Optimisez votre présence en ligne — Oreo Studios",
          "body": "Bonjour,\n\nNous avons analysé votre site web et identifié plusieurs opportunités.\n\nCordialement,\nOreo Studios",
          "summary": "Email de premier contact pour améliorer la présence digitale.",
          "callToAction": "Accepteriez-vous un appel de 15 minutes ?"
        }
        """;
        _testResult = testResult;
    }

    public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default)
        => Task.FromResult(new AiCompletionResult(
            Content: _responseOverride,
            Model: "mock-model-1.0",
            PromptTokens: 100,
            CompletionTokens: 50,
            TotalTokens: 150,
            GenerationMs: 42));

    public Task<bool> TestConnectionAsync(CancellationToken ct = default)
        => Task.FromResult(_testResult);
}
