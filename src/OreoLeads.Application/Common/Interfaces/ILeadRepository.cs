using OreoLeads.Application.Common.Models;
using OreoLeads.Application.Features.Leads.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface ILeadRepository
{
    Task<PagedResult<LeadSummaryDto>> SearchAsync(LeadFilterDto filter, CancellationToken ct = default);
    Task<LeadDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Lead> CreateAsync(Lead lead, CancellationToken ct = default);
    Task UpdateAsync(Lead lead, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<Lead?> GetEntityByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<LeadSummaryDto>> GetByFilterForExportAsync(LeadFilterDto filter, CancellationToken ct = default);
    Task<int> BulkImportAsync(List<Lead> leads, CancellationToken ct = default);
}
