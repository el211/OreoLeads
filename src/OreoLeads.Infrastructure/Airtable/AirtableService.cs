using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Airtable.DTOs;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Airtable;

internal sealed class AirtableService : IAirtableService
{
    private const string BaseUrl     = "https://api.airtable.com";
    private const string DataBase    = "/v0";
    private const string MetaBase    = "/v0/meta";
    private static readonly int[] RetryableStatusCodes = [429, 500, 502, 503, 504];

    private readonly HttpClient             _http;
    private readonly ILogger<AirtableService> _logger;

    public AirtableService(HttpClient http, ILogger<AirtableService> logger)
    {
        _http   = http;
        _logger = logger;
    }

    // ── Connection test ───────────────────────────────────────────────────────

    public async Task<AirtableTestResultDto> TestConnectionAsync(
        string accessToken, string baseId, CancellationToken ct = default)
    {
        try
        {
            using var req  = BuildRequest(HttpMethod.Get, $"{MetaBase}/bases/{baseId}/tables", accessToken);
            using var resp = await ExecuteWithRetryAsync(req, accessToken, ct);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                return new AirtableTestResultDto(false, "Invalid access token.", null, null);
            if (resp.StatusCode == HttpStatusCode.Forbidden)
                return new AirtableTestResultDto(false, "Access denied to this base.", null, null);
            if (resp.StatusCode == HttpStatusCode.NotFound)
                return new AirtableTestResultDto(false, "Base not found.", null, null);

            if (!resp.IsSuccessStatusCode)
                return new AirtableTestResultDto(false, $"Airtable API returned {(int)resp.StatusCode}.", null, null);

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            string? baseName = null;
            if (json.TryGetProperty("name", out var nameEl))
                baseName = nameEl.GetString();

            _logger.LogInformation("Airtable TestConnection succeeded for base {BaseId}.", baseId);
            return new AirtableTestResultDto(true, "Connection successful.", null, baseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Airtable TestConnection failed for base {BaseId}.", baseId);
            return new AirtableTestResultDto(false, ex.Message, null, null);
        }
    }

    // ── Tables ────────────────────────────────────────────────────────────────

    public async Task<List<AirtableTableDto>> GetTablesAsync(
        string accessToken, string baseId, CancellationToken ct = default)
    {
        using var req  = BuildRequest(HttpMethod.Get, $"{MetaBase}/bases/{baseId}/tables", accessToken);
        using var resp = await ExecuteWithRetryAsync(req, accessToken, ct);
        await EnsureSuccessAsync(resp, ct);

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var tables = new List<AirtableTableDto>();

        if (json.TryGetProperty("tables", out var arr))
        {
            foreach (var t in arr.EnumerateArray())
            {
                var id   = t.TryGetProperty("id",          out var idEl)   ? idEl.GetString()   ?? "" : "";
                var name = t.TryGetProperty("name",        out var nameEl) ? nameEl.GetString() ?? "" : "";
                var desc = t.TryGetProperty("description", out var descEl) ? descEl.GetString()      : null;
                tables.Add(new AirtableTableDto(id, name, desc));
            }
        }

        return tables;
    }

    // ── Fields ────────────────────────────────────────────────────────────────

    public async Task<List<AirtableFieldDto>> GetFieldsAsync(
        string accessToken, string baseId, string tableIdOrName, CancellationToken ct = default)
    {
        using var req  = BuildRequest(HttpMethod.Get, $"{MetaBase}/bases/{baseId}/tables", accessToken);
        using var resp = await ExecuteWithRetryAsync(req, accessToken, ct);
        await EnsureSuccessAsync(resp, ct);

        var json   = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var fields = new List<AirtableFieldDto>();

        if (!json.TryGetProperty("tables", out var tables)) return fields;

        foreach (var t in tables.EnumerateArray())
        {
            var tId   = t.TryGetProperty("id",   out var idEl)   ? idEl.GetString()   : null;
            var tName = t.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;

            if (tId != tableIdOrName && tName != tableIdOrName) continue;

            if (!t.TryGetProperty("fields", out var fieldsArr)) break;

            foreach (var f in fieldsArr.EnumerateArray())
            {
                var fId   = f.TryGetProperty("id",   out var fidEl)   ? fidEl.GetString()   ?? "" : "";
                var fName = f.TryGetProperty("name", out var fnameEl) ? fnameEl.GetString() ?? "" : "";
                var fType = f.TryGetProperty("type", out var ftypeEl) ? ftypeEl.GetString()      : null;
                fields.Add(new AirtableFieldDto(fId, fName, MapFieldType(fType)));
            }
            break;
        }

        return fields;
    }

    // ── Records ───────────────────────────────────────────────────────────────

    public async Task<AirtableRecordsPageDto> ListRecordsAsync(
        string accessToken, string baseId, string tableIdOrName,
        string? offset, string? filterFormula, string? view, int pageSize,
        CancellationToken ct = default)
    {
        var url = $"{DataBase}/{baseId}/{Uri.EscapeDataString(tableIdOrName)}?pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(offset))
            url += $"&offset={Uri.EscapeDataString(offset)}";
        if (!string.IsNullOrWhiteSpace(filterFormula))
            url += $"&filterByFormula={Uri.EscapeDataString(filterFormula)}";
        if (!string.IsNullOrWhiteSpace(view))
            url += $"&view={Uri.EscapeDataString(view)}";

        using var req  = BuildRequest(HttpMethod.Get, url, accessToken);
        using var resp = await ExecuteWithRetryAsync(req, accessToken, ct);
        await EnsureSuccessAsync(resp, ct);

        var json    = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var records = ParseRecords(json);
        string? nextOffset = json.TryGetProperty("offset", out var offEl) ? offEl.GetString() : null;

        return new AirtableRecordsPageDto(records, nextOffset);
    }

    public async Task<AirtableRecordDto?> GetRecordAsync(
        string accessToken, string baseId, string tableIdOrName, string recordId,
        CancellationToken ct = default)
    {
        using var req  = BuildRequest(HttpMethod.Get,
            $"{DataBase}/{baseId}/{Uri.EscapeDataString(tableIdOrName)}/{recordId}", accessToken);
        using var resp = await ExecuteWithRetryAsync(req, accessToken, ct);

        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureSuccessAsync(resp, ct);

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return ParseRecord(json);
    }

    // ── Create / Update ───────────────────────────────────────────────────────

    public async Task<string> CreateRecordAsync(
        string accessToken, string baseId, string tableIdOrName,
        Dictionary<string, object?> fields, CancellationToken ct = default)
    {
        var ids = await CreateRecordsBatchAsync(accessToken, baseId, tableIdOrName,
            [fields], ct);
        return ids[0];
    }

    public async Task UpdateRecordAsync(
        string accessToken, string baseId, string tableIdOrName, string recordId,
        Dictionary<string, object?> fields, CancellationToken ct = default)
    {
        await UpdateRecordsBatchAsync(accessToken, baseId, tableIdOrName,
            new Dictionary<string, Dictionary<string, object?>> { [recordId] = fields }, ct);
    }

    public async Task<List<string>> CreateRecordsBatchAsync(
        string accessToken, string baseId, string tableIdOrName,
        List<Dictionary<string, object?>> records, CancellationToken ct = default)
    {
        var ids = new List<string>();

        // Airtable batch limit: 10 records per request
        for (var i = 0; i < records.Count; i += 10)
        {
            var batch = records.Skip(i).Take(10).ToList();
            var body  = JsonSerializer.Serialize(new
            {
                records = batch.Select(f => new { fields = f }).ToList()
            });

            using var req = BuildRequestWithContent(HttpMethod.Post,
                $"{DataBase}/{baseId}/{Uri.EscapeDataString(tableIdOrName)}",
                accessToken,
                new StringContent(body, Encoding.UTF8, "application/json"));
            using var resp = await ExecuteWithRetryAsync(req, accessToken, ct);
            await EnsureSuccessAsync(resp, ct);

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (json.TryGetProperty("records", out var recs))
                foreach (var r in recs.EnumerateArray())
                    if (r.TryGetProperty("id", out var idEl))
                        ids.Add(idEl.GetString() ?? "");
        }

        return ids;
    }

    public async Task UpdateRecordsBatchAsync(
        string accessToken, string baseId, string tableIdOrName,
        Dictionary<string, Dictionary<string, object?>> recordUpdates, CancellationToken ct = default)
    {
        var entries = recordUpdates.ToList();

        for (var i = 0; i < entries.Count; i += 10)
        {
            var batch = entries.Skip(i).Take(10).ToList();
            var body  = JsonSerializer.Serialize(new
            {
                records = batch.Select(kv => new { id = kv.Key, fields = kv.Value }).ToList()
            });

            using var req = BuildRequestWithContent(HttpMethod.Patch,
                $"{DataBase}/{baseId}/{Uri.EscapeDataString(tableIdOrName)}",
                accessToken,
                new StringContent(body, Encoding.UTF8, "application/json"));
            using var resp = await ExecuteWithRetryAsync(req, accessToken, ct);
            await EnsureSuccessAsync(resp, ct);
        }
    }

    // ── Webhooks ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an Airtable webhook. Note: Airtable uses a ping-then-poll model.
    /// When a change occurs, Airtable sends a ping to notificationUrl, then the app
    /// must call GetWebhookChangesAsync to retrieve the actual payload.
    /// </summary>
    public async Task<AirtableWebhookDto?> CreateWebhookAsync(
        string accessToken, string baseId, string notificationUrl, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            notificationUrl,
            specification = new
            {
                options = new
                {
                    filters = new
                    {
                        fromSources = new[] { "client" },
                        dataTypes   = new[] { "tableData" },
                        recordChangeScope = (string?)null
                    }
                }
            }
        });

        using var req = BuildRequestWithContent(HttpMethod.Post,
            $"{DataBase}/bases/{baseId}/webhooks",
            accessToken,
            new StringContent(body, Encoding.UTF8, "application/json"));
        using var resp = await ExecuteWithRetryAsync(req, accessToken, ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Airtable CreateWebhook failed: {Status}", resp.StatusCode);
            return null;
        }

        var json   = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var id     = json.TryGetProperty("id",             out var idEl)     ? idEl.GetString()     ?? "" : "";
        var notUrl = json.TryGetProperty("notificationUrl",out var notUrlEl) ? notUrlEl.GetString() ?? "" : notificationUrl;
        string? cursor = json.TryGetProperty("cursor", out var curEl) ? curEl.GetString() : null;

        DateTime? expiry = null;
        if (json.TryGetProperty("expirationTime", out var expEl) && expEl.ValueKind != JsonValueKind.Null)
        {
            if (expEl.TryGetDateTime(out var dt)) expiry = dt;
        }

        return new AirtableWebhookDto(id, notUrl, expiry, cursor);
    }

    public async Task RefreshWebhookAsync(
        string accessToken, string baseId, string webhookId, CancellationToken ct = default)
    {
        using var req = BuildRequestWithContent(HttpMethod.Post,
            $"{DataBase}/bases/{baseId}/webhooks/{webhookId}/refresh",
            accessToken,
            new StringContent("{}", Encoding.UTF8, "application/json"));
        using var resp = await ExecuteWithRetryAsync(req, accessToken, ct);

        if (!resp.IsSuccessStatusCode)
            _logger.LogWarning("Airtable RefreshWebhook failed: {Status}", resp.StatusCode);
    }

    public async Task DeleteWebhookAsync(
        string accessToken, string baseId, string webhookId, CancellationToken ct = default)
    {
        using var req  = BuildRequest(HttpMethod.Delete,
            $"{DataBase}/bases/{baseId}/webhooks/{webhookId}", accessToken);
        using var resp = await ExecuteWithRetryAsync(req, accessToken, ct);

        if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NotFound)
            _logger.LogWarning("Airtable DeleteWebhook failed: {Status}", resp.StatusCode);
    }

    public async Task<AirtableWebhookChangesDto> GetWebhookChangesAsync(
        string accessToken, string baseId, string webhookId, string? cursor,
        CancellationToken ct = default)
    {
        var url = $"{DataBase}/bases/{baseId}/webhooks/{webhookId}/payloads";
        if (!string.IsNullOrWhiteSpace(cursor))
            url += $"?cursor={Uri.EscapeDataString(cursor)}";

        using var req  = BuildRequest(HttpMethod.Get, url, accessToken);
        using var resp = await ExecuteWithRetryAsync(req, accessToken, ct);
        await EnsureSuccessAsync(resp, ct);

        var json          = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var newCursor     = json.TryGetProperty("cursor",       out var cEl)  ? cEl.GetString()  ?? "" : "";
        var mightHaveMore = json.TryGetProperty("mightHaveMore",out var mhEl) && mhEl.GetBoolean();

        var changes = new List<AirtableWebhookChangeDto>();
        if (json.TryGetProperty("payloads", out var payloads))
        {
            foreach (var payload in payloads.EnumerateArray())
            {
                if (!payload.TryGetProperty("changedTablesById", out var tables)) continue;

                foreach (var tableEntry in tables.EnumerateObject())
                {
                    var tableId = tableEntry.Name;
                    var tData   = tableEntry.Value;

                    if (tData.TryGetProperty("createdRecordsById", out var created))
                        foreach (var r in created.EnumerateObject())
                            changes.Add(new AirtableWebhookChangeDto(tableId, r.Name, "create", null));

                    if (tData.TryGetProperty("destroyedRecordIds", out var destroyed))
                        foreach (var rid in destroyed.EnumerateArray())
                            changes.Add(new AirtableWebhookChangeDto(tableId, rid.GetString(), "delete", null));

                    if (tData.TryGetProperty("changedRecordsById", out var updated))
                    {
                        foreach (var r in updated.EnumerateObject())
                        {
                            Dictionary<string, object?>? changedFields = null;
                            if (r.Value.TryGetProperty("current", out var cur) &&
                                cur.TryGetProperty("cellValuesByFieldId", out var cells))
                            {
                                changedFields = new Dictionary<string, object?>();
                                foreach (var cell in cells.EnumerateObject())
                                    changedFields[cell.Name] = cell.Value.ValueKind == JsonValueKind.Null
                                        ? null : (object?)cell.Value.GetRawText();
                            }
                            changes.Add(new AirtableWebhookChangeDto(tableId, r.Name, "update", changedFields));
                        }
                    }
                }
            }
        }

        return new AirtableWebhookChangesDto(newCursor, mightHaveMore, changes);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static HttpRequestMessage BuildRequest(HttpMethod method, string path, string accessToken)
    {
        var msg = new HttpRequestMessage(method, BaseUrl + path);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return msg;
    }

    private static HttpRequestMessage BuildRequestWithContent(
        HttpMethod method, string path, string accessToken, HttpContent content)
    {
        var msg = BuildRequest(method, path, accessToken);
        msg.Content = content;
        return msg;
    }

    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        HttpRequestMessage request, string accessToken, CancellationToken ct)
    {
        HttpResponseMessage? response = null;
        var delays = new[] { 1, 2, 4 };

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var req = attempt == 0 ? request : CloneRequest(request, accessToken);

            _logger.LogDebug(
                "Airtable HTTP {Method} {Url} attempt {Attempt}",
                req.Method, req.RequestUri, attempt + 1);

            response = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!RetryableStatusCodes.Contains((int)response.StatusCode))
                return response;

            if (attempt == 2) break;

            var delay = delays[attempt];

            // Respect Retry-After header on 429
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? delay;
                delay = (int)Math.Min(retryAfter, 30);
            }

            _logger.LogWarning(
                "Airtable API returned {StatusCode} on attempt {Attempt}. Retrying in {Delay}s...",
                (int)response.StatusCode, attempt + 1, delay);

            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(delay), ct);
        }

        return response!;
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage original, string accessToken)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        clone.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (original.Content is not null)
        {
            var bytes    = original.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            var ct       = original.Content.Headers.ContentType?.MediaType ?? "application/json";
            clone.Content = new StringContent(Encoding.UTF8.GetString(bytes), Encoding.UTF8, ct);
        }

        return clone;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode) return;

        var body = await resp.Content.ReadAsStringAsync(ct);

        throw resp.StatusCode switch
        {
            HttpStatusCode.Unauthorized  => new InvalidOperationException("Invalid access token."),
            HttpStatusCode.Forbidden     => new InvalidOperationException("Access denied to this resource."),
            HttpStatusCode.NotFound      => new InvalidOperationException("Resource not found."),
            HttpStatusCode.BadRequest    => new InvalidOperationException($"Bad request: {body}"),
            _                            => new InvalidOperationException(
                                               $"Airtable API error {(int)resp.StatusCode}: {body}")
        };
    }

    private static List<AirtableRecordDto> ParseRecords(JsonElement json)
    {
        var list = new List<AirtableRecordDto>();
        if (!json.TryGetProperty("records", out var recs)) return list;
        foreach (var r in recs.EnumerateArray())
            list.Add(ParseRecord(r));
        return list;
    }

    private static AirtableRecordDto ParseRecord(JsonElement r)
    {
        var id = r.TryGetProperty("id",          out var idEl)  ? idEl.GetString()  ?? "" : "";
        var fields = new Dictionary<string, object?>();

        if (r.TryGetProperty("fields", out var fieldsEl))
            foreach (var f in fieldsEl.EnumerateObject())
                fields[f.Name] = f.Value.ValueKind == JsonValueKind.Null
                    ? null : (object?)f.Value.GetRawText();

        DateTime? created = null, modified = null;
        if (r.TryGetProperty("createdTime", out var ct) && ct.TryGetDateTime(out var ctDt))
            created = ctDt;

        return new AirtableRecordDto(id, fields, created, modified);
    }

    private static AirtableFieldType MapFieldType(string? type) => type switch
    {
        "email"          => AirtableFieldType.Email,
        "phoneNumber"    => AirtableFieldType.PhoneNumber,
        "url"            => AirtableFieldType.Url,
        "number"         => AirtableFieldType.Number,
        "checkbox"       => AirtableFieldType.Checkbox,
        "singleSelect"   => AirtableFieldType.SingleSelect,
        "multipleSelects"=> AirtableFieldType.MultipleSelects,
        "date"           => AirtableFieldType.Date,
        "dateTime"       => AirtableFieldType.DateTime,
        "multilineText"  => AirtableFieldType.MultilineText,
        _                => AirtableFieldType.SingleLineText,
    };
}
