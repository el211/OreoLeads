using System.Text;
using System.Text.Json;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;

namespace OreoLeads.Infrastructure.Automation.Actions;

internal sealed class HttpRequestActionHandler : IActionHandler
{
    private readonly HttpClient _httpClient;

    public HttpRequestActionHandler(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<ActionResultDto> ExecuteAsync(string? configJson, AutomationContext context, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(configJson))
                return new ActionResultDto(false, null, "No HTTP request configuration", sw.ElapsedMilliseconds);

            using var doc = JsonDocument.Parse(configJson);
            var root = doc.RootElement;

            var method = root.TryGetProperty("method", out var m) ? m.GetString() ?? "GET" : "GET";
            var url = root.TryGetProperty("url", out var u) ? u.GetString() : null;
            var body = root.TryGetProperty("body", out var b) ? b.GetString() : null;

            if (string.IsNullOrWhiteSpace(url))
                return new ActionResultDto(false, null, "URL is required", sw.ElapsedMilliseconds);

            url = context.InterpolateString(url);
            if (body is not null) body = context.InterpolateString(body);

            var request = new HttpRequestMessage(new HttpMethod(method), url);

            // Add headers
            if (root.TryGetProperty("headers", out var headers) && headers.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in headers.EnumerateObject())
                {
                    request.Headers.TryAddWithoutValidation(prop.Name, context.InterpolateString(prop.Value.GetString() ?? ""));
                }
            }

            if (body is not null)
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            return new ActionResultDto(
                response.IsSuccessStatusCode,
                responseBody,
                response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return new ActionResultDto(false, null, ex.Message, sw.ElapsedMilliseconds);
        }
    }
}
