using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Persistence.Repositories;

namespace OreoLeads.Infrastructure.Ai;

internal sealed class AiConfigurationService : IAiConfigurationService
{
    private readonly ApplicationDbContext _db;
    private readonly IEncryptionService   _encryption;

    public AiConfigurationService(ApplicationDbContext db, IEncryptionService encryption)
    {
        _db         = db;
        _encryption = encryption;
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

        existing.ProviderType   = dto.ProviderType;
        existing.Model          = dto.Model;
        existing.Temperature    = dto.Temperature;
        existing.MaxTokens      = dto.MaxTokens;
        existing.TimeoutSeconds = dto.TimeoutSeconds;
        existing.IsEnabled      = dto.IsEnabled;
        existing.BaseUrl        = dto.BaseUrl;

        if (!string.IsNullOrWhiteSpace(dto.ApiKey))
            existing.EncryptedApiKey = EncryptApiKey(dto.ApiKey);

        existing.SetUpdatedAt();
        await _db.SaveChangesAsync();
        return existing;
    }

    /// <summary>Delegates to the shared AES-256-GCM encryption service.</summary>
    public string EncryptApiKey(string plainKey) => _encryption.Encrypt(plainKey);

    /// <summary>Delegates to the shared AES-256-GCM decryption service.</summary>
    public string DecryptApiKey(string encryptedKey) => _encryption.Decrypt(encryptedKey);

    public string? GetDecryptedApiKey(AiConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.EncryptedApiKey)) return null;
        try { return DecryptApiKey(config.EncryptedApiKey); }
        catch { return null; }
    }

    public async Task SeedDefaultPromptsAsync()
    {
        var repo      = new PromptTemplateRepository(_db);
        var templates = DefaultPrompts.GetAll();
        foreach (var t in templates)
        {
            var existing = await repo.GetByKeyAsync(t.Key);
            if (existing is null)
                await repo.UpsertAsync(t);
        }
    }
}
