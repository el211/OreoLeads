using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Brevo.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Brevo;

internal sealed class BrevoConfigurationService : IBrevoConfigurationService
{
    private readonly ApplicationDbContext _db;
    private readonly IBrevoService        _brevo;
    private readonly IEncryptionService   _encryption;

    public BrevoConfigurationService(
        ApplicationDbContext db,
        IBrevoService        brevo,
        IEncryptionService   encryption)
    {
        _db         = db;
        _brevo      = brevo;
        _encryption = encryption;
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

        existing.SenderName    = dto.SenderName;
        existing.SenderEmail   = dto.SenderEmail;
        existing.ReplyTo       = dto.ReplyTo;
        existing.IsEnabled     = dto.IsEnabled;
        existing.TestMode      = dto.TestMode;
        existing.TestModeEmail = dto.TestModeEmail;
        existing.DailyLimit    = dto.DailyLimit;

        if (!string.IsNullOrWhiteSpace(dto.ApiKey))
            existing.EncryptedApiKey = _encryption.Encrypt(dto.ApiKey);

        if (!string.IsNullOrWhiteSpace(dto.WebhookSecret))
            existing.WebhookSecret = _encryption.Encrypt(dto.WebhookSecret);

        existing.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public string? GetDecryptedApiKey(BrevoConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.EncryptedApiKey)) return null;
        try { return _encryption.Decrypt(config.EncryptedApiKey); }
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
}
