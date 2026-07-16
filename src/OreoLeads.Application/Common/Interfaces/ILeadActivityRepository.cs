using OreoLeads.Application.Features.LeadActivities.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface ILeadActivityRepository
{
    Task<List<LeadActivityDto>> GetByLeadIdAsync(Guid leadId, CancellationToken ct = default);
    Task<LeadActivity> AddAsync(LeadActivity activity, CancellationToken ct = default);
}
