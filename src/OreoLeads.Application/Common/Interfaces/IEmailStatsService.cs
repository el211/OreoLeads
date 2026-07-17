using OreoLeads.Application.Features.Brevo.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface IEmailStatsService
{
    Task<EmailStatsDto> GetStatsAsync(
        Guid?     organizationId = null,
        DateTime? from           = null,
        DateTime? to             = null,
        CancellationToken ct     = default);
}
