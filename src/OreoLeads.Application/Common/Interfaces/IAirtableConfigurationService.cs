using OreoLeads.Application.Features.Airtable.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface IAirtableConfigurationService
{
    Task<AirtableConfiguration?> GetCurrentAsync(Guid? organizationId, CancellationToken ct = default);
    Task<AirtableConfiguration> SaveAsync(UpdateAirtableConfigurationDto dto, Guid? organizationId, CancellationToken ct = default);
    string? GetDecryptedAccessToken(AirtableConfiguration config);
    Task<AirtableTestResultDto> TestConnectionAsync(Guid? organizationId, CancellationToken ct = default);
    Task<List<AirtableTableDto>> GetTablesAsync(Guid? organizationId, CancellationToken ct = default);
    Task<List<AirtableFieldDto>> GetFieldsAsync(Guid? organizationId, string tableIdOrName, CancellationToken ct = default);
    Task<List<AirtableFieldMapping>> GetMappingsAsync(Guid configId, CancellationToken ct = default);
    Task SaveMappingsAsync(Guid configId, List<SaveAirtableFieldMappingDto> mappings, CancellationToken ct = default);
}
