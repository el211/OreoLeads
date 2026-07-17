using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Brevo.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Brevo;

internal sealed class BrevoConfigurationService : IBrevoConfigurationService
{
    private readonly ApplicationDbContext _db;
    private readonly IBrevoService        _brevo;
    private readonly byte[]               _encKey;
    private readonly byte[]               _encIv;

    public BrevoConfigurationService(
        ApplicationDbContext db,
        IBrevoService        brevo,
        IConfiguration       configuration)
    {
        _db    = db;
        _brevo = brevo;

        var secret = configuration["Brevo:EncryptionKey"] ?? "OreoLeadsBrevoDefaultSecretKey32!";
        using var sha = SHA256.Create();
        _encKey = sha.ComputeHash(Encoding.UTF8.GetBytes(secret)); // 32 bytes
        _encIv  = _encKey[..16];                                   // 16 bytes
    }

    public async Task<BrevoConfiguration?> GetCurrentAsync(CancellationToken ct = default)
        => await _db.Set<BrevoConfiguration>()
                    .OrderBy(x => x.CreatedAt)
                    .FirstOrDefaultAsync(ct);

    public async Task<BrevoConfiguration> SaveAsync(UpdateBrevoConfigurationDto dto, CancellationToken ct = default)
    {
        var existing = await GetCurrentAsync(ct);

        if (existing is null)
        {
            existing = new BrevoConfiguration();
            _db.Set<BrevoConfiguration>().Add(existing);
        }

        existing.SenderName   = dto.SenderName;
        existing.SenderEmail  = dto.SenderEmail;
        existing.ReplyTo      = dto.ReplyTo;
        existing.IsEnabled    = dto.IsEnabled;
        existing.TestMode     = dto.TestMode;
        existing.TestModeEmail = dto.TestModeEmail;
        existing.DailyLimit   = dto.DailyLimit;

        if (!string.IsNullOrWhiteSpace(dto.ApiKey))
            existing.EncryptedApiKey = EncryptApiKey(dto.ApiKey);

        if (!string.IsNullOrWhiteSpace(dto.WebhookSecret))
            existing.WebhookSecret = EncryptApiKey(dto.WebhookSecret);

        existing.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public string? GetDecryptedApiKey(BrevoConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.EncryptedApiKey)) return null;
        try { return DecryptApiKey(config.EncryptedApiKey); }
        catch { return null; }
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

    // ── Encryption (AES-256-CBC — mirrors AiConfigurationService exactly) ─────

    internal string EncryptApiKey(string plainKey)
    {
        using var aes = Aes.Create();
        aes.Key = _encKey;
        aes.IV  = _encIv;

        using var encryptor = aes.CreateEncryptor();
        var bytes     = Encoding.UTF8.GetBytes(plainKey);
        var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(encrypted);
    }

    internal string DecryptApiKey(string encryptedKey)
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
