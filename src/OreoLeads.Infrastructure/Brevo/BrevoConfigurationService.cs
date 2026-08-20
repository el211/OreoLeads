using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Brevo.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Brevo;

internal sealed class BrevoConfigurationService : IBrevoConfigurationService
{
    // Legacy CBC secret used before the migration to the shared GCM service.
    // Kept so that existing database rows encrypted with the old algorithm remain readable.
    // Rows are silently re-encrypted to GCM on the next SaveAsync call.
    private const string LegacyCbcFallbackDefault = "OreoLeadsBrevoDefaultSecretKey32!";

    private readonly ApplicationDbContext _db;
    private readonly IBrevoService        _brevo;
    private readonly IEncryptionService   _encryption;
    private readonly string               _legacyCbcSecret;

    public BrevoConfigurationService(
        ApplicationDbContext db,
        IBrevoService        brevo,
        IEncryptionService   encryption,
        IConfiguration       configuration)
    {
        _db              = db;
        _brevo           = brevo;
        _encryption      = encryption;
        _legacyCbcSecret = configuration["Brevo:EncryptionKey"] ?? LegacyCbcFallbackDefault;
    }

    public async Task<BrevoConfiguration?> GetCurrentAsync(CancellationToken ct = default)
    {
        var config = await _db.Set<BrevoConfiguration>()
                              .OrderBy(x => x.CreatedAt)
                              .FirstOrDefaultAsync(ct);
        if (config is null) return null;

        // Auto-migrate any legacy (unversioned) encrypted fields to gcm:v1: on first read.
        // This covers both old raw-GCM and old CBC values. Idempotent: versioned values skipped.
        bool needsSave = false;

        if (!string.IsNullOrWhiteSpace(config.EncryptedApiKey) && !_encryption.IsVersioned(config.EncryptedApiKey))
        {
            var plain = _encryption.TryDecryptWithCbcFallback(config.EncryptedApiKey, _legacyCbcSecret);
            if (plain is not null) { config.EncryptedApiKey = _encryption.Encrypt(plain); needsSave = true; }
        }

        if (!string.IsNullOrWhiteSpace(config.WebhookSecret) && !_encryption.IsVersioned(config.WebhookSecret))
        {
            var plain = _encryption.TryDecryptWithCbcFallback(config.WebhookSecret, _legacyCbcSecret);
            if (plain is not null) { config.WebhookSecret = _encryption.Encrypt(plain); needsSave = true; }
        }

        if (needsSave)
        {
            config.SetUpdatedAt();
            await _db.SaveChangesAsync(ct);
        }

        return config;
    }

    public async Task<BrevoConfiguration> SaveAsync(UpdateBrevoConfigurationDto dto, CancellationToken ct = default)
    {
        var existing = await GetCurrentAsync(ct);

        if (existing is null)
        {
            existing = new BrevoConfiguration();
            _db.Set<BrevoConfiguration>().Add(existing);
        }

        existing.SenderName    = dto.SenderName;
        existing.SenderEmail   = dto.SenderEmail;
        existing.ReplyTo       = dto.ReplyTo;
        existing.ContactPhone  = dto.ContactPhone;
        existing.IsEnabled     = dto.IsEnabled;
        existing.TestMode      = dto.TestMode;
        existing.TestModeEmail = dto.TestModeEmail;
        existing.DailyLimit    = dto.DailyLimit;

        // Always re-encrypt with GCM when a new value is provided.
        // This is also what migrates any legacy CBC value stored in the DB:
        // the user saves → new GCM ciphertext replaces the old CBC one.
        if (!string.IsNullOrWhiteSpace(dto.ApiKey))
            existing.EncryptedApiKey = _encryption.Encrypt(dto.ApiKey.Trim());

        if (!string.IsNullOrWhiteSpace(dto.WebhookSecret))
            existing.WebhookSecret = _encryption.Encrypt(dto.WebhookSecret);

        existing.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    /// <summary>
    /// Decrypts the stored API key. Tries GCM first (new format); if that fails,
    /// falls back to the legacy AES-256-CBC algorithm so rows written before the
    /// migration to the shared encryption service remain readable.
    /// </summary>
    public string? GetDecryptedApiKey(BrevoConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.EncryptedApiKey)) return null;
        return _encryption.TryDecryptWithCbcFallback(config.EncryptedApiKey, _legacyCbcSecret);
    }

    public async Task<BrevoTestResultDto> TestConnectionAsync(CancellationToken ct = default)
    {
        var config = await GetCurrentAsync(ct);
        if (config is null)
            return new BrevoTestResultDto(false, "Brevo n'est pas configuré.", null, null);

        var apiKey = GetDecryptedApiKey(config);
        if (string.IsNullOrWhiteSpace(apiKey))
            return new BrevoTestResultDto(false, "Clé API Brevo introuvable ou corrompue.", null, null);

        return await _brevo.TestConnectionAsync(apiKey, ct);
    }

    public Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}
