using OreoLeads.Application.Features.Ai.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface IPromptBuilder
{
    Task<string> BuildEmailSystemPromptAsync(Guid leadId, GenerateEmailRequestDto request, CancellationToken ct = default);
    Task<string> BuildEmailUserPromptAsync(Guid leadId, GenerateEmailRequestDto request, CancellationToken ct = default);
}
