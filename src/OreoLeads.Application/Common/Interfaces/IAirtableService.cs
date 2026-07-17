using OreoLeads.Application.Features.Airtable.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface IAirtableService
{
    Task<AirtableTestResultDto> TestConnectionAsync(string accessToken, string baseId, CancellationToken ct = default);
    Task<List<AirtableTableDto>> GetTablesAsync(string accessToken, string baseId, CancellationToken ct = default);
    Task<List<AirtableFieldDto>> GetFieldsAsync(string accessToken, string baseId, string tableIdOrName, CancellationToken ct = default);
    Task<AirtableRecordsPageDto> ListRecordsAsync(string accessToken, string baseId, string tableIdOrName, string? offset, string? filterFormula, string? view, int pageSize, CancellationToken ct = default);
    Task<AirtableRecordDto?> GetRecordAsync(string accessToken, string baseId, string tableIdOrName, string recordId, CancellationToken ct = default);
    Task<string> CreateRecordAsync(string accessToken, string baseId, string tableIdOrName, Dictionary<string, object?> fields, CancellationToken ct = default);
    Task UpdateRecordAsync(string accessToken, string baseId, string tableIdOrName, string recordId, Dictionary<string, object?> fields, CancellationToken ct = default);
    Task<List<string>> CreateRecordsBatchAsync(string accessToken, string baseId, string tableIdOrName, List<Dictionary<string, object?>> records, CancellationToken ct = default);
    Task UpdateRecordsBatchAsync(string accessToken, string baseId, string tableIdOrName, Dictionary<string, Dictionary<string, object?>> recordUpdates, CancellationToken ct = default);
    Task<AirtableWebhookDto?> CreateWebhookAsync(string accessToken, string baseId, string notificationUrl, CancellationToken ct = default);
    Task RefreshWebhookAsync(string accessToken, string baseId, string webhookId, CancellationToken ct = default);
    Task DeleteWebhookAsync(string accessToken, string baseId, string webhookId, CancellationToken ct = default);
    Task<AirtableWebhookChangesDto> GetWebhookChangesAsync(string accessToken, string baseId, string webhookId, string? cursor, CancellationToken ct = default);
}
