using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Persistence.Repositories;

namespace OreoLeads.Infrastructure.Ai;

internal sealed class AiConfigurationService : IAiConfigurationService
{
    private readonly ApplicationDbContext _db;
    private readonly byte[] _encKey;
    private readonly byte[] _encIv;

    public AiConfigurationService(ApplicationDbContext db, IConfiguration configuration)
    {
        _db = db;

        // Derive a 256-bit key and 128-bit IV from the configured secret
        var secret = configuration["Ai:EncryptionKey"] ?? "OreoLeadsDefaultSecretKey_32chars!";
        using var sha = SHA256.Create();
        _encKey = sha.ComputeHash(Encoding.UTF8.GetBytes(secret));  // 32 bytes
        _encIv  = _encKey[..16];                                    // 16 bytes
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

    public string EncryptApiKey(string plainKey)
    {
        using var aes = Aes.Create();
        aes.Key = _encKey;
        aes.IV  = _encIv;

        using var encryptor = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plainKey);
        var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(encrypted);
    }

    public string DecryptApiKey(string encryptedKey)
    {
        using var aes = Aes.Create();
        aes.Key = _encKey;
        aes.IV  = _encIv;

        using var decryptor = aes.CreateDecryptor();
        var bytes = Convert.FromBase64String(encryptedKey);
        var decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Encoding.UTF8.GetString(decrypted);
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
