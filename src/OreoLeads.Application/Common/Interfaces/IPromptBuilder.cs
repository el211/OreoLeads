using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Application.Features.Sms.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface IPromptBuilder
{
    Task<string> BuildEmailSystemPromptAsync(Guid leadId, GenerateEmailRequestDto request, CancellationToken ct = default);
    Task<string> BuildEmailUserPromptAsync(Guid leadId, GenerateEmailRequestDto request, CancellationToken ct = default);

    Task<string> BuildSmsSystemPromptAsync(CancellationToken ct = default);
    Task<string> BuildSmsUserPromptAsync(Guid leadId, GenerateSmsRequestDto request, string? contactPhone, string? contactEmail, CancellationToken ct = default);
}
