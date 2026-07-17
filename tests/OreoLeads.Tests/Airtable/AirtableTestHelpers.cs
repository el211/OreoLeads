using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Airtable.DTOs;

namespace OreoLeads.Tests.Airtable;

/// <summary>Shared stub for IAirtableService used across Airtable tests.</summary>
internal sealed class StubAirtableService : IAirtableService
{
    public Task<AirtableTestResultDto> TestConnectionAsync(string a, string b, CancellationToken ct = default)
        => Task.FromResult(new AirtableTestResultDto(true, "OK", null, null));

    public Task<List<AirtableTableDto>> GetTablesAsync(string a, string b, CancellationToken ct = default)
        => Task.FromResult(new List<AirtableTableDto>());

    public Task<List<AirtableFieldDto>> GetFieldsAsync(string a, string b, string c, CancellationToken ct = default)
        => Task.FromResult(new List<AirtableFieldDto>());

    public Task<AirtableRecordsPageDto> ListRecordsAsync(
        string a, string b, string c, string? d, string? e, string? f, int g, CancellationToken ct = default)
        => Task.FromResult(new AirtableRecordsPageDto([], null));

    public Task<AirtableRecordDto?> GetRecordAsync(string a, string b, string c, string d, CancellationToken ct = default)
        => Task.FromResult<AirtableRecordDto?>(null);

    public Task<string> CreateRecordAsync(string a, string b, string c,
        Dictionary<string, object?> d, CancellationToken ct = default)
        => Task.FromResult("recTest");

    public Task UpdateRecordAsync(string a, string b, string c, string d,
        Dictionary<string, object?> e, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<List<string>> CreateRecordsBatchAsync(string a, string b, string c,
        List<Dictionary<string, object?>> d, CancellationToken ct = default)
        => Task.FromResult(new List<string>());

    public Task UpdateRecordsBatchAsync(string a, string b, string c,
        Dictionary<string, Dictionary<string, object?>> d, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<AirtableWebhookDto?> CreateWebhookAsync(string a, string b, string c, CancellationToken ct = default)
        => Task.FromResult<AirtableWebhookDto?>(null);

    public Task RefreshWebhookAsync(string a, string b, string c, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteWebhookAsync(string a, string b, string c, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<AirtableWebhookChangesDto> GetWebhookChangesAsync(
        string a, string b, string c, string? d, CancellationToken ct = default)
        => Task.FromResult(new AirtableWebhookChangesDto("cursor", false, []));
}
