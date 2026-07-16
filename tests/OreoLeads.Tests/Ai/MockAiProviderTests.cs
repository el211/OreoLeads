using FluentAssertions;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Tests.Ai;

/// <summary>
/// Tests of the mock provider — validates that it fulfils the IAiProvider contract.
/// In production, no provider should call real APIs in tests.
/// </summary>
public class MockAiProviderTests
{
    private readonly MockAiProvider _provider = new();

    [Fact]
    public void MockProvider_IsConfigured()
        => _provider.IsConfigured.Should().BeTrue();

    [Fact]
    public void MockProvider_ProviderType_IsClaude()
        => _provider.ProviderType.Should().Be(AiProviderType.Claude);

    [Fact]
    public async Task CompleteAsync_ReturnsNonEmptyContent()
    {
        var result = await _provider.CompleteAsync(
            new AiCompletionRequest("system", "user"));

        result.Content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CompleteAsync_ReturnsTotalTokens()
    {
        var result = await _provider.CompleteAsync(
            new AiCompletionRequest("system", "user"));

        result.TotalTokens.Should().Be(result.PromptTokens + result.CompletionTokens);
    }

    [Fact]
    public async Task CompleteAsync_GenerationMs_IsPositive()
    {
        var result = await _provider.CompleteAsync(
            new AiCompletionRequest("system", "user"));

        result.GenerationMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TestConnectionAsync_ReturnsTrue()
    {
        var ok = await _provider.TestConnectionAsync();
        ok.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_WhenFailProvider_ReturnsFalse()
    {
        var failProvider = new MockAiProvider(testResult: false);
        var ok = await failProvider.TestConnectionAsync();
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteAsync_WithCustomResponse_ReturnsCustomContent()
    {
        var custom = new MockAiProvider("""{"subject":"Custom","body":"Custom body","summary":null,"callToAction":null}""");
        var result = await custom.CompleteAsync(new AiCompletionRequest("sys", "usr"));
        result.Content.Should().Contain("Custom");
    }

    [Fact]
    public async Task MultipleCompleteAsync_ReturnsSameContent()
    {
        var r1 = await _provider.CompleteAsync(new AiCompletionRequest("s", "u"));
        var r2 = await _provider.CompleteAsync(new AiCompletionRequest("s", "u"));
        r1.Content.Should().Be(r2.Content);
    }
}
