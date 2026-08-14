using OreoLeads.Application.Features.Sms.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface ISmsGeneratorService
{
    Task<GenerateSmsResponseDto> GenerateAsync(Guid leadId, GenerateSmsRequestDto request, CancellationToken ct = default);
}
