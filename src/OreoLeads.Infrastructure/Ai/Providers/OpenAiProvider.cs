using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Ai.Providers;

internal class OpenAiProvider : IAiProvider
{
    private readonly IAiConfigurationService _configService;
    private readonly HttpClient _http;

    protected virtual string BaseUrl => "https://api.openai.com/v1/chat/completions";
    public virtual string ProviderName => "OpenAI";
    public virtual AiProviderType ProviderType => AiProviderType.OpenAI;

    public bool IsConfigured
    {
        get
        {
            var cfg = _configService.GetCurrentAsync().GetAwaiter().GetResult();
            return cfg is not null && cfg.ProviderType == ProviderType
                   && !string.IsNullOrWhiteSpace(cfg.EncryptedApiKey);
        }
    }

    public OpenAiProvider(IAiConfigurationService configService, HttpClient http)
    {
        _configService = configService;
        _http = http;
    }

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default)
    {
        var cfg = await _configService.GetCurrentAsync()
                  ?? throw new InvalidOperationException("AI is not configured.");

        var apiKey = _configService.GetDecryptedApiKey(cfg)
                     ?? throw new InvalidOperationException("OpenAI API key is missing.");

        var model = cfg.Model ?? "gpt-4o-mini";

        // GPT-5.x uses the Responses API (/v1/responses) instead of Chat Completions.
        return IsResponsesApiModel(model)
            ? await CompleteViaResponsesApiAsync(request, apiKey, model, cfg.BaseUrl, ct)
            : await CompleteViaChatCompletionsAsync(request, apiKey, model, cfg.BaseUrl, ct);
    }

    private static bool IsResponsesApiModel(string model)
        => model.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase)
        || model.StartsWith("o3",    StringComparison.OrdinalIgnoreCase)
        || model.StartsWith("o4",    StringComparison.OrdinalIgnoreCase);

    // ── Chat Completions API (GPT-4o, GPT-4, etc.) ────────────────────────────

    private async Task<AiCompletionResult> CompleteViaChatCompletionsAsync(
        AiCompletionRequest request, string apiKey, string model, string? baseUrl, CancellationToken ct)
    {
        var endpoint = !string.IsNullOrWhiteSpace(baseUrl)
            ? baseUrl.TrimEnd('/') + "/chat/completions"
            : BaseUrl;

        var body = new
        {
            model,
            max_tokens = request.MaxTokens,
            temperature = request.Temperature,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = JsonContent.Create(body);

        var sw = Stopwatch.StartNew();
        using var resp = await _http.SendAsync(req, ct);
        sw.Stop();

        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<OpenAiResponse>(ct)
                   ?? throw new InvalidOperationException("Empty response from OpenAI.");

        var content = json.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;

        return new AiCompletionResult(
            Content: content,
            Model: json.Model ?? model,
            PromptTokens: json.Usage?.PromptTokens ?? 0,
            CompletionTokens: json.Usage?.CompletionTokens ?? 0,
            TotalTokens: json.Usage?.TotalTokens ?? 0,
            GenerationMs: (int)sw.ElapsedMilliseconds);
    }

    // ── Responses API (GPT-5.x) ───────────────────────────────────────────────

    private async Task<AiCompletionResult> CompleteViaResponsesApiAsync(
        AiCompletionRequest request, string apiKey, string model, string? baseUrl, CancellationToken ct)
    {
        var endpoint = !string.IsNullOrWhiteSpace(baseUrl)
            ? baseUrl.TrimEnd('/') + "/responses"
            : "https://api.openai.com/v1/responses";

        var body = new
        {
            model,
            max_output_tokens = request.MaxTokens,
            input = new object[]
            {
                new { role = "developer", content = request.SystemPrompt },
                new { role = "user",      content = request.UserPrompt  }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = JsonContent.Create(body);

        var sw = Stopwatch.StartNew();
        using var resp = await _http.SendAsync(req, ct);
        sw.Stop();

        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<OpenAiResponsesApiResponse>(ct)
                   ?? throw new InvalidOperationException("Empty response from OpenAI Responses API.");

        // Extract text from output[0].content[0].text
        var text = json.Output?
            .SelectMany(o => o.Content ?? [])
            .FirstOrDefault(c => c.Type == "output_text")
            ?.Text ?? string.Empty;

        return new AiCompletionResult(
            Content: text,
            Model: json.Model ?? model,
            PromptTokens: json.Usage?.InputTokens ?? 0,
            CompletionTokens: json.Usage?.OutputTokens ?? 0,
            TotalTokens: json.Usage?.TotalTokens ?? 0,
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

    // ── Chat Completions response models ──────────────────────────────────────

    private sealed class OpenAiResponse
    {
        [JsonPropertyName("model")]   public string?        Model   { get; set; }
        [JsonPropertyName("choices")] public OpenAiChoice[]? Choices { get; set; }
        [JsonPropertyName("usage")]   public OpenAiUsage?   Usage   { get; set; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")] public OpenAiMessage? Message { get; set; }
    }

    private sealed class OpenAiMessage
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }

    private sealed class OpenAiUsage
    {
        [JsonPropertyName("prompt_tokens")]     public int PromptTokens     { get; set; }
        [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
        [JsonPropertyName("total_tokens")]      public int TotalTokens      { get; set; }
    }

    // ── Responses API response models (GPT-5.x) ───────────────────────────────

    private sealed class OpenAiResponsesApiResponse
    {
        [JsonPropertyName("model")]  public string?                   Model  { get; set; }
        [JsonPropertyName("output")] public OpenAiResponsesOutput[]?  Output { get; set; }
        [JsonPropertyName("usage")]  public OpenAiResponsesUsage?     Usage  { get; set; }
    }

    private sealed class OpenAiResponsesOutput
    {
        [JsonPropertyName("content")] public OpenAiResponsesContent[]? Content { get; set; }
    }

    private sealed class OpenAiResponsesContent
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("text")] public string? Text { get; set; }
    }

    private sealed class OpenAiResponsesUsage
    {
        [JsonPropertyName("input_tokens")]  public int InputTokens  { get; set; }
        [JsonPropertyName("output_tokens")] public int OutputTokens { get; set; }
        [JsonPropertyName("total_tokens")]  public int TotalTokens  { get; set; }
    }
}
