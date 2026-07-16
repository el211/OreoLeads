using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Ai.Providers;

internal sealed class OllamaProvider : IAiProvider
{
    private readonly IAiConfigurationService _configService;
    private readonly HttpClient _http;

    public string ProviderName => "Ollama (local)";
    public AiProviderType ProviderType => AiProviderType.Ollama;

    public bool IsConfigured
    {
        get
        {
            var cfg = _configService.GetCurrentAsync().GetAwaiter().GetResult();
            return cfg is not null && cfg.ProviderType == AiProviderType.Ollama;
        }
    }

    public OllamaProvider(IAiConfigurationService configService, HttpClient http)
    {
        _configService = configService;
        _http = http;
    }

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default)
    {
        var cfg = await _configService.GetCurrentAsync()
                  ?? throw new InvalidOperationException("AI is not configured.");

        var model = cfg.Model ?? "llama3.2";
        var baseUrl = !string.IsNullOrWhiteSpace(cfg.BaseUrl)
            ? cfg.BaseUrl.TrimEnd('/')
            : "http://localhost:11434";

        var endpoint = $"{baseUrl}/api/chat";

        var body = new
        {
            model,
            stream = false,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            }
        };

        var sw = Stopwatch.StartNew();
        using var resp = await _http.PostAsJsonAsync(endpoint, body, ct);
        sw.Stop();

        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<OllamaResponse>(ct)
                   ?? throw new InvalidOperationException("Empty response from Ollama.");

        var content = json.Message?.Content ?? string.Empty;

        return new AiCompletionResult(
            Content: content,
            Model: json.Model ?? model,
            PromptTokens: json.PromptEvalCount,
            CompletionTokens: json.EvalCount,
            TotalTokens: json.PromptEvalCount + json.EvalCount,
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

    private sealed class OllamaResponse
    {
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("message")] public OllamaMessage? Message { get; set; }
        [JsonPropertyName("prompt_eval_count")] public int PromptEvalCount { get; set; }
        [JsonPropertyName("eval_count")] public int EvalCount { get; set; }
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
}
