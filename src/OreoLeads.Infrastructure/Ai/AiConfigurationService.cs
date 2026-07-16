using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Persistence.Repositories;

namespace OreoLeads.Infrastructure.Ai;

internal sealed class AiConfigurationService : IAiConfigurationService
{
    private readonly ApplicationDbContext _db;
    private readonly byte[] _encKey;

    public AiConfigurationService(ApplicationDbContext db, IConfiguration configuration)
    {
        _db = db;

        // Derive a 256-bit key from the configured secret
        var secret = configuration["Ai:EncryptionKey"] ?? "OreoLeadsDefaultSecretKey_32chars!";
        using var sha = SHA256.Create();
        _encKey = sha.ComputeHash(Encoding.UTF8.GetBytes(secret));  // 32 bytes
    }

    public async Task<AiConfiguration?> GetCurrentAsync()
        => await _db.Set<AiConfiguration>().OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();

    public async Task<AiConfiguration> SaveAsync(UpdateAiConfigurationDto dto)
    {
        var existing = await GetCurrentAsync();

        if (existing is null)
        {
            existing = new AiConfiguration();
            _db.Set<AiConfiguration>().Add(existing);
        }

        existing.ProviderType    = dto.ProviderType;
        existing.Model           = dto.Model;
        existing.Temperature     = dto.Temperature;
        existing.MaxTokens       = dto.MaxTokens;
        existing.TimeoutSeconds  = dto.TimeoutSeconds;
        existing.IsEnabled       = dto.IsEnabled;
        existing.BaseUrl         = dto.BaseUrl;

        if (!string.IsNullOrWhiteSpace(dto.ApiKey))
            existing.EncryptedApiKey = EncryptApiKey(dto.ApiKey);

        existing.SetUpdatedAt();
        await _db.SaveChangesAsync();
        return existing;
    }

    /// <summary>
    /// Encrypts using AES-256-GCM with a random 12-byte nonce.
    /// Output format: Base64(nonce[12] + tag[16] + ciphertext)
    /// </summary>
    public string EncryptApiKey(string plainKey)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainKey);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];      // 12 bytes
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];           // 16 bytes
        var ciphertext = new byte[plainBytes.Length];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_encKey, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plainBytes, ciphertext, tag);

        // Concat: nonce(12) + tag(16) + ciphertext
        var combined = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(combined);
    }

    /// <summary>Decrypts AES-256-GCM ciphertext produced by EncryptApiKey.</summary>
    public string DecryptApiKey(string encryptedKey)
    {
        var combined = Convert.FromBase64String(encryptedKey);

        var nonce = combined[..12];
        var tag = combined[12..28];
        var ciphertext = combined[28..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_encKey, AesGcm.TagByteSizes.MaxSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    public string? GetDecryptedApiKey(AiConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.EncryptedApiKey)) return null;
        try { return DecryptApiKey(config.EncryptedApiKey); }
        catch { return null; }
    }

    public async Task SeedDefaultPromptsAsync()
    {
        var repo = new PromptTemplateRepository(_db);
        var templates = DefaultPrompts.GetAll();
        foreach (var t in templates)
        {
            var existing = await repo.GetByKeyAsync(t.Key);
            if (existing is null)
                await repo.UpsertAsync(t);
        }
    }
}
