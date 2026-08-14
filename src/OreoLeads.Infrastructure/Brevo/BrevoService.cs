using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Brevo.DTOs;

namespace OreoLeads.Infrastructure.Brevo;

internal sealed class BrevoService : IBrevoService
{
    private const string BaseUrl = "https://api.brevo.com/v3";
    private static readonly int[] RetryableStatusCodes = [429, 500, 502, 503, 504];

    private readonly HttpClient _http;
    private readonly ILogger<BrevoService> _logger;

    public BrevoService(HttpClient http, ILogger<BrevoService> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<BrevoTestResultDto> TestConnectionAsync(string apiKey, CancellationToken ct = default)
    {
        try
        {
            var keyPreview = string.IsNullOrWhiteSpace(apiKey)
                ? "(vide)"
                : apiKey[..Math.Min(12, apiKey.Length)] + "… (len=" + apiKey.Length + ")";
            _logger.LogWarning("Brevo TestConnection: clé utilisée = '{KeyPreview}'", keyPreview);

            using var request = BuildRequest(HttpMethod.Get, "/account", apiKey);
            using var response = await ExecuteWithRetryAsync(request, apiKey, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Brevo TestConnection failed: {StatusCode}", response.StatusCode);
                return new BrevoTestResultDto(false, $"Brevo API returned {(int)response.StatusCode}", null, null);
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

            var firstName  = json.TryGetProperty("firstName",  out var fn) ? fn.GetString() : null;
            var lastName   = json.TryGetProperty("lastName",   out var ln) ? ln.GetString() : null;
            var accountName = $"{firstName} {lastName}".Trim();

            string? planName = null;
            if (json.TryGetProperty("plan", out var plans) && plans.ValueKind == JsonValueKind.Array)
            {
                foreach (var plan in plans.EnumerateArray())
                {
                    if (plan.TryGetProperty("type", out var type))
                    {
                        planName = type.GetString();
                        break;
                    }
                }
            }

            _logger.LogInformation("Brevo TestConnection succeeded. Account: {Account}", accountName);
            return new BrevoTestResultDto(true, "Connection successful.", accountName, planName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo TestConnection threw an exception");
            return new BrevoTestResultDto(false, ex.Message, null, null);
        }
    }

    public async Task<string> SendEmailAsync(EmailSendRequest request, CancellationToken ct = default)
    {
        try
        {
            var body = new Dictionary<string, object?>
            {
                ["sender"]      = new { name = request.SenderName, email = request.SenderEmail },
                ["to"]          = new[] { new { email = request.ToEmail, name = request.ToName } },
                ["subject"]     = request.Subject,
                ["htmlContent"] = request.HtmlBody,
            };

            if (!string.IsNullOrWhiteSpace(request.ReplyTo))
                body["replyTo"] = new { email = request.ReplyTo };

            var tags = request.Tags?.ToList();
            if (tags is { Count: > 0 })
                body["tags"] = tags;

            if (request.LeadId.HasValue)
                body["headers"] = new Dictionary<string, string> { ["X-Lead-Id"] = request.LeadId.Value.ToString() };

            var json    = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpRequest = BuildRequestWithContent(HttpMethod.Post, "/smtp/email", request.ApiKey, content);
            using var response    = await ExecuteWithRetryAsync(httpRequest, request.ApiKey, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"Brevo SendEmail failed ({(int)response.StatusCode}): {error}");
            }

            var result    = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var messageId = result.TryGetProperty("messageId", out var mid) ? mid.GetString() : null;

            _logger.LogInformation(
                "Brevo email sent to {ToEmail}. MessageId: {MessageId}",
                request.ToEmail, messageId);

            return messageId ?? string.Empty;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Brevo SendEmail error: {ex.Message}", ex);
        }
    }

    public async Task<string> SendSmsAsync(SmsSendRequest request, CancellationToken ct = default)
    {
        try
        {
            var body = new Dictionary<string, object?>
            {
                ["sender"]    = request.SenderName,
                ["recipient"] = request.ToPhone,
                ["content"]   = request.Message,
                ["type"]      = "transactional"
            };

            var json    = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpRequest = BuildRequestWithContent(
                HttpMethod.Post, "/transactionalSMS/sms", request.ApiKey, content);
            using var response = await ExecuteWithRetryAsync(httpRequest, request.ApiKey, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"Brevo SendSms failed ({(int)response.StatusCode}): {error}");
            }

            var result    = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var messageId = result.TryGetProperty("messageId", out var mid) ? mid.ToString() : null;

            _logger.LogInformation("Brevo SMS sent to {ToPhone}. MessageId: {MessageId}",
                request.ToPhone, messageId);

            return messageId ?? string.Empty;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Brevo SendSms error: {ex.Message}", ex);
        }
    }

    public async Task SyncContactAsync(ContactSyncRequest request, CancellationToken ct = default)
    {
        try
        {
            var attributes = new Dictionary<string, object?>();
            if (!string.IsNullOrWhiteSpace(request.FirstName)) attributes["FIRSTNAME"] = request.FirstName;
            if (!string.IsNullOrWhiteSpace(request.LastName))  attributes["LASTNAME"]  = request.LastName;
            if (!string.IsNullOrWhiteSpace(request.Phone))     attributes["PHONE"]     = request.Phone;
            if (!string.IsNullOrWhiteSpace(request.Company))   attributes["COMPANY"]   = request.Company;
            if (!string.IsNullOrWhiteSpace(request.City))      attributes["CITY"]      = request.City;
            if (!string.IsNullOrWhiteSpace(request.Sector))    attributes["SECTOR"]    = request.Sector;

            var body = new Dictionary<string, object?>
            {
                ["email"]      = request.Email,
                ["attributes"] = attributes
            };

            if (request.Lists is not null)
            {
                var listIds = request.Lists
                    .Where(l => int.TryParse(l, out _))
                    .Select(int.Parse)
                    .ToList();
                if (listIds.Count > 0)
                    body["listIds"] = listIds;
            }

            var json    = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var encoded = Uri.EscapeDataString(request.Email);

            using var httpRequest = BuildRequestWithContent(HttpMethod.Put, $"/contacts/{encoded}", request.ApiKey, content);
            using var response    = await ExecuteWithRetryAsync(httpRequest, request.ApiKey, ct);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"Brevo SyncContact failed ({(int)response.StatusCode}): {error}");
            }

            _logger.LogInformation("Brevo contact synced: {Email}", request.Email);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Brevo SyncContact error: {ex.Message}", ex);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static HttpRequestMessage BuildRequest(HttpMethod method, string path, string apiKey)
    {
        var msg = new HttpRequestMessage(method, BaseUrl + path);
        msg.Headers.Add("api-key", apiKey);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return msg;
    }

    private static HttpRequestMessage BuildRequestWithContent(
        HttpMethod method, string path, string apiKey, HttpContent content)
    {
        var msg = BuildRequest(method, path, apiKey);
        msg.Content = content;
        return msg;
    }

    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        HttpRequestMessage request, string apiKey, CancellationToken ct)
    {
        HttpResponseMessage? response = null;
        var delays = new[] { 1, 2, 4 };

        for (var attempt = 0; attempt < 3; attempt++)
        {
            // HttpRequestMessage can only be sent once — clone after first attempt
            var req = attempt == 0
                ? request
                : await CloneRequestAsync(request, apiKey);

            _logger.LogDebug(
                "Brevo HTTP {Method} {Url} attempt {Attempt}",
                req.Method, req.RequestUri, attempt + 1);

            response = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!RetryableStatusCodes.Contains((int)response.StatusCode))
                return response;

            if (attempt == 2)
                break;

            var delay = delays[attempt];
            _logger.LogWarning(
                "Brevo API returned {StatusCode} on attempt {Attempt}. Retrying in {Delay}s...",
                (int)response.StatusCode, attempt + 1, delay);

            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(delay), ct);
        }

        return response!;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original, string apiKey)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        clone.Headers.Add("api-key", apiKey);
        clone.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (original.Content is not null)
        {
            var bytes   = await original.Content.ReadAsByteArrayAsync();
            var ct      = original.Content.Headers.ContentType?.MediaType ?? "application/json";
            var charset = original.Content.Headers.ContentType?.CharSet   ?? "utf-8";
            clone.Content = new StringContent(
                Encoding.UTF8.GetString(bytes), Encoding.UTF8, ct);
        }

        return clone;
    }
}
