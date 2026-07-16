using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface IAiConfigurationService
{
    Task<AiConfiguration?> GetCurrentAsync();
    Task<AiConfiguration> SaveAsync(UpdateAiConfigurationDto dto);
    string EncryptApiKey(string plainKey);
    string DecryptApiKey(string encryptedKey);
    string? GetDecryptedApiKey(AiConfiguration config);
    Task SeedDefaultPromptsAsync();
}
