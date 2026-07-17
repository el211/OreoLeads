using OreoLeads.Application.Features.Brevo.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface IBrevoConfigurationService
{
    Task<BrevoConfiguration?> GetCurrentAsync(CancellationToken ct = default);
    Task<BrevoConfiguration>  SaveAsync(UpdateBrevoConfigurationDto dto, CancellationToken ct = default);
    string?                   GetDecryptedApiKey(BrevoConfiguration config);
    Task<BrevoTestResultDto>  TestConnectionAsync(CancellationToken ct = default);
    /// <summary>No-op seed — exists for consistency with other configuration services.</summary>
    Task                      SeedAsync(CancellationToken ct = default);
}
