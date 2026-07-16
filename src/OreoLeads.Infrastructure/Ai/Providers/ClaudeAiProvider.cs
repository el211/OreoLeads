using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Ai.Providers;

internal sealed class ClaudeAiProvider : IAiProvider
{
    private readonly IAiConfigurationService _configService;
    private readonly HttpClient _http;

    public string ProviderName => "Claude (Anthropic)";
    public AiProviderType ProviderType => AiProviderType.Claude;

    public bool IsConfigured
    {
        get
        {
            var cfg = _configService.GetCurrentAsync().GetAwaiter().GetResult();
            return cfg is not null && cfg.ProviderType == AiProviderType.Claude
                   && !string.IsNullOrWhiteSpace(cfg.EncryptedApiKey);
        }
    }

    public ClaudeAiProvider(IAiConfigurationService configService, HttpClient http)
    {
        _configService = configService;
        _http = http;
    }

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default)
    {
        var cfg = await _configService.GetCurrentAsync()
                  ?? throw new InvalidOperationException("AI is not configured.");

        var apiKey = _configService.GetDecryptedApiKey(cfg)
                     ?? throw new InvalidOperationException("Claude API key is missing.");

        var model = cfg.Model ?? "claude-sonnet-4-5";

        var body = new
        {
            model,
            max_tokens = request.MaxTokens,
            system = request.SystemPrompt,
            messages = new[] { new { role = "user", content = request.UserPrompt } }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = JsonContent.Create(body);

        var sw = Stopwatch.StartNew();
        using var resp = await _http.SendAsync(req, ct);
        sw.Stop();

        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<ClaudeResponse>(ct)
                   ?? throw new InvalidOperationException("Empty response from Claude.");

        var content = json.Content?.FirstOrDefault()?.Text ?? string.Empty;

        return new AiCompletionResult(
            Content: content,
            Model: json.Model ?? model,
            PromptTokens: json.Usage?.InputTokens ?? 0,
            CompletionTokens: json.Usage?.OutputTokens ?? 0,
            TotalTokens: (json.Usage?.InputTokens ?? 0) + (json.Usage?.OutputTokens ?? 0),
            GenerationMs: (int)sw.ElapsedMilliseconds);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await CompleteAsync(
                new AiCompletionRequest("You are a helpful assistant.", "Reply with: OK", 5, 0.1f), ct);
            return !string.IsNullOrWhiteSpace(result.Content);
        }
        catch
        {
            return false;
        }
    }

    // ── Internal response models ──────────────────────────────────────────────

    private sealed class ClaudeResponse
    {
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("content")] public ClaudeContent[]? Content { get; set; }
        [JsonPropertyName("usage")] public ClaudeUsage? Usage { get; set; }
    }

    private sealed class ClaudeContent
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
    }

    private sealed class ClaudeUsage
    {
        [JsonPropertyName("input_tokens")] public int InputTokens { get; set; }
        [JsonPropertyName("output_tokens")] public int OutputTokens { get; set; }
    }
}
