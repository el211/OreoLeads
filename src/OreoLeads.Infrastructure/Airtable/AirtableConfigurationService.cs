using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Airtable.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Airtable;

internal sealed class AirtableConfigurationService : IAirtableConfigurationService
{
    // Legacy CBC secret used before the migration to the shared GCM service.
    private const string LegacyCbcFallbackDefault = "OreoLeadsAirtableDefaultSecretKey!";

    private readonly ApplicationDbContext _db;
    private readonly IAirtableService     _airtable;
    private readonly IEncryptionService   _encryption;
    private readonly string               _legacyCbcSecret;

    public AirtableConfigurationService(
        ApplicationDbContext db,
        IAirtableService     airtable,
        IEncryptionService   encryption,
        IConfiguration       configuration)
    {
        _db              = db;
        _airtable        = airtable;
        _encryption      = encryption;
        _legacyCbcSecret = configuration["Airtable:EncryptionKey"] ?? LegacyCbcFallbackDefault;
    }

    public async Task<AirtableConfiguration?> GetCurrentAsync(
        Guid? organizationId, CancellationToken ct = default)
    {
        var config = await _db.Set<AirtableConfiguration>()
                              .Include(x => x.FieldMappings)
                              .OrderBy(x => x.CreatedAt)
                              .FirstOrDefaultAsync(ct);
        if (config is null) return null;

        // Auto-migrate any legacy (unversioned) access token to gcm:v1: on first read.
        // Idempotent: versioned values are skipped unconditionally.
        if (!string.IsNullOrWhiteSpace(config.EncryptedAccessToken) &&
            !_encryption.IsVersioned(config.EncryptedAccessToken))
        {
            var plain = _encryption.TryDecryptWithCbcFallback(config.EncryptedAccessToken, _legacyCbcSecret);
            if (plain is not null)
            {
                config.EncryptedAccessToken = _encryption.Encrypt(plain);
                config.SetUpdatedAt();
                await _db.SaveChangesAsync(ct);
            }
        }

        return config;
    }

    public async Task<AirtableConfiguration> SaveAsync(
        UpdateAirtableConfigurationDto dto, Guid? organizationId, CancellationToken ct = default)
    {
        var existing = await GetCurrentAsync(organizationId, ct);

        if (existing is null)
        {
            existing = new AirtableConfiguration
            {
                OrganizationId = organizationId
            };
            _db.Set<AirtableConfiguration>().Add(existing);
        }

        existing.ConnectionName  = dto.ConnectionName;
        existing.BaseId          = dto.BaseId;
        existing.TableIdOrName   = dto.TableIdOrName;
        existing.IsEnabled       = dto.IsEnabled;
        existing.SyncDirection   = dto.SyncDirection;
        existing.ConflictStrategy = dto.ConflictStrategy;

        if (!string.IsNullOrWhiteSpace(dto.AccessToken))
            existing.EncryptedAccessToken = _encryption.Encrypt(dto.AccessToken);

        existing.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    /// <summary>
    /// Decrypts the stored access token. Tries GCM first (new format); if that fails,
    /// falls back to the legacy AES-256-CBC algorithm so rows written before the
    /// migration to the shared encryption service remain readable.
    /// </summary>
    public string? GetDecryptedAccessToken(AirtableConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.EncryptedAccessToken)) return null;
        return _encryption.TryDecryptWithCbcFallback(config.EncryptedAccessToken, _legacyCbcSecret);
    }

    public async Task<AirtableTestResultDto> TestConnectionAsync(
        Guid? organizationId, CancellationToken ct = default)
    {
        var config = await GetCurrentAsync(organizationId, ct);
        if (config is null)
            return new AirtableTestResultDto(false, "Airtable is not configured.", null, null);

        var token = GetDecryptedAccessToken(config);
        if (string.IsNullOrWhiteSpace(token))
            return new AirtableTestResultDto(false, "Access token not found or corrupted.", null, null);

        return await _airtable.TestConnectionAsync(token, config.BaseId, ct);
    }

    public async Task<List<AirtableTableDto>> GetTablesAsync(
        Guid? organizationId, CancellationToken ct = default)
    {
        var config = await GetCurrentAsync(organizationId, ct);
        if (config is null) return [];

        var token = GetDecryptedAccessToken(config);
        if (string.IsNullOrWhiteSpace(token)) return [];

        return await _airtable.GetTablesAsync(token, config.BaseId, ct);
    }

    public async Task<List<AirtableFieldDto>> GetFieldsAsync(
        Guid? organizationId, string tableIdOrName, CancellationToken ct = default)
    {
        var config = await GetCurrentAsync(organizationId, ct);
        if (config is null) return [];

        var token = GetDecryptedAccessToken(config);
        if (string.IsNullOrWhiteSpace(token)) return [];

        return await _airtable.GetFieldsAsync(token, config.BaseId, tableIdOrName, ct);
    }

    public async Task<List<AirtableFieldMapping>> GetMappingsAsync(
        Guid configId, CancellationToken ct = default)
        => await _db.Set<AirtableFieldMapping>()
                    .Where(x => x.AirtableConfigurationId == configId)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync(ct);

    public async Task SaveMappingsAsync(
        Guid configId, List<SaveAirtableFieldMappingDto> mappings, CancellationToken ct = default)
    {
        // Delete existing
        var existing = await _db.Set<AirtableFieldMapping>()
                                .Where(x => x.AirtableConfigurationId == configId)
                                .ToListAsync(ct);
        _db.Set<AirtableFieldMapping>().RemoveRange(existing);

        // Insert new
        foreach (var m in mappings)
        {
            _db.Set<AirtableFieldMapping>().Add(new AirtableFieldMapping
            {
                AirtableConfigurationId = configId,
                OreoLeadsField          = m.OreoLeadsField,
                AirtableFieldName       = m.AirtableFieldName,
                AirtableFieldType       = m.AirtableFieldType,
                Direction               = m.Direction,
                IsRequired              = m.IsRequired,
                DefaultValue            = m.DefaultValue,
                Transformation          = m.Transformation,
                SortOrder               = m.SortOrder,
            });
        }

        await _db.SaveChangesAsync(ct);
    }

}
