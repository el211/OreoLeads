using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Airtable.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Airtable;

internal sealed class AirtableConfigurationService : IAirtableConfigurationService
{
    private readonly ApplicationDbContext _db;
    private readonly IAirtableService     _airtable;
    private readonly byte[]               _encKey;
    private readonly byte[]               _encIv;

    public AirtableConfigurationService(
        ApplicationDbContext db,
        IAirtableService     airtable,
        IConfiguration       configuration)
    {
        _db       = db;
        _airtable = airtable;

        var secret = configuration["Airtable:EncryptionKey"] ?? "OreoLeadsAirtableDefaultSecretKey!";
        using var sha = SHA256.Create();
        _encKey = sha.ComputeHash(Encoding.UTF8.GetBytes(secret)); // 32 bytes
        _encIv  = _encKey[..16];                                    // 16 bytes
    }

    public async Task<AirtableConfiguration?> GetCurrentAsync(
        Guid? organizationId, CancellationToken ct = default)
        => await _db.Set<AirtableConfiguration>()
                    .Include(x => x.FieldMappings)
                    .OrderBy(x => x.CreatedAt)
                    .FirstOrDefaultAsync(ct);

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
            existing.EncryptedAccessToken = EncryptToken(dto.AccessToken);

        existing.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public string? GetDecryptedAccessToken(AirtableConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.EncryptedAccessToken)) return null;
        try { return DecryptToken(config.EncryptedAccessToken); }
        catch { return null; }
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

    // ── Encryption (AES-256-CBC) ──────────────────────────────────────────────

    internal string EncryptToken(string plainKey)
    {
        using var aes = Aes.Create();
        aes.Key = _encKey;
        aes.IV  = _encIv;
        using var encryptor = aes.CreateEncryptor();
        var bytes     = Encoding.UTF8.GetBytes(plainKey);
        var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(encrypted);
    }

    internal string DecryptToken(string encryptedKey)
    {
        using var aes = Aes.Create();
        aes.Key = _encKey;
        aes.IV  = _encIv;
        using var decryptor = aes.CreateDecryptor();
        var bytes     = Convert.FromBase64String(encryptedKey);
        var decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Encoding.UTF8.GetString(decrypted);
    }
}
