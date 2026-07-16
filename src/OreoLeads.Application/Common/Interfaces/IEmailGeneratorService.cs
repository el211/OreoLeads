using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface IEmailGeneratorService
{
    Task<GeneratedEmail> GenerateAsync(Guid leadId, GenerateEmailRequestDto request, CancellationToken ct = default);
    Task<EmailDraftVersion> RegenerateAsync(Guid draftId, CancellationToken ct = default);
}
