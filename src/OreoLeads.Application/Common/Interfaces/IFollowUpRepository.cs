using OreoLeads.Application.Features.FollowUps.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface IFollowUpRepository
{
    Task<List<FollowUpDto>> GetByLeadIdAsync(Guid leadId, CancellationToken ct = default);
    Task<List<FollowUpDto>> GetPendingAsync(CancellationToken ct = default);
    Task<List<FollowUpDto>> GetOverdueAsync(CancellationToken ct = default);
    Task<FollowUp> CreateAsync(FollowUp followUp, CancellationToken ct = default);
    Task UpdateAsync(FollowUp followUp, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<FollowUp?> GetEntityByIdAsync(Guid id, CancellationToken ct = default);
}
