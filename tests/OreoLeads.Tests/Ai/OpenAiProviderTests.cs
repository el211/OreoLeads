using System.Net;
using System.Text;
using System.Text.Json;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Ai.Providers;

namespace OreoLeads.Tests.Ai;

public class OpenAiProviderTests
{
    [Theory]
    [InlineData("gpt-6-sol")]
    [InlineData("gpt-6-luna")]
    [InlineData("gpt-6-astra")]
    [InlineData("gpt-6.1-sol")]
    public async Task Gpt6_UsesResponsesWithoutSamplingParameters(string model)
    {
        using var handler = new CaptureHandler();
        using var http = new HttpClient(handler);
        var provider = new OpenAiProvider(new ConfigService(model), http);

        var result = await provider.CompleteAsync(new AiCompletionRequest("system", "user", 1000, 0.7f));

        Assert.Equal("https://api.openai.com/v1/responses", handler.Url);
        var body = handler.Body;
        Assert.Equal(model, body.GetProperty("model").GetString());
        Assert.Equal("low", body.GetProperty("reasoning").GetProperty("effort").GetString());
        Assert.Equal(1000, body.GetProperty("max_output_tokens").GetInt32());
        Assert.False(body.TryGetProperty("temperature", out _));
        Assert.False(body.TryGetProperty("max_tokens", out _));
        Assert.Equal("OK", result.Content);
        Assert.Equal(12, result.TotalTokens);
        Assert.True(await provider.TestConnectionAsync());
        Assert.Equal(1024, handler.Body.GetProperty("max_output_tokens").GetInt32());
    }

    [Theory]
    [InlineData("gpt-4o", "/chat/completions", "max_tokens")]
    [InlineData("gpt-5", "/responses", "max_output_tokens")]
    [InlineData("o3", "/responses", "max_output_tokens")]
    public async Task ExistingModels_KeepTheirRequestFormat(string model, string path, string tokenField)
    {
        using var handler = new CaptureHandler();
        using var http = new HttpClient(handler);
        var provider = new OpenAiProvider(new ConfigService(model), http);
        await provider.CompleteAsync(new AiCompletionRequest("system", "user", 1000, 0.7f));
        Assert.EndsWith(path, handler.Url);
        Assert.Equal(1000, handler.Body.GetProperty(tokenField).GetInt32());
        Assert.False(handler.Body.TryGetProperty("reasoning", out _));
        Assert.Equal(model == "gpt-4o", handler.Body.TryGetProperty("temperature", out _));
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? Url { get; private set; }
        public JsonElement Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Url = request.RequestUri!.ToString();
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Body = document.RootElement.Clone();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"output":[{"type":"reasoning"},{"content":[{"type":"output_text","text":"O"},{"type":"output_text","text":"K"}]}],"choices":[{"message":{"content":"OK"}}],"usage":{"input_tokens":2,"output_tokens":10,"total_tokens":12}}
                    """, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class ConfigService(string model) : IAiConfigurationService
    {
        public Task<AiConfiguration?> GetCurrentAsync() => Task.FromResult<AiConfiguration?>(new() { Model = model });
        public string? GetDecryptedApiKey(AiConfiguration config) => "test-key";
        public Task<AiConfiguration> SaveAsync(UpdateAiConfigurationDto dto) => throw new NotSupportedException();
        public string EncryptApiKey(string plainKey) => throw new NotSupportedException();
        public string DecryptApiKey(string encryptedKey) => throw new NotSupportedException();
        public Task SeedDefaultPromptsAsync() => Task.CompletedTask;
    }
}
