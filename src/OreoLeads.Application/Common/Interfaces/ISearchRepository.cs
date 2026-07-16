using OreoLeads.Application.Common.Models;
using OreoLeads.Application.Features.Search.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface ISearchRepository
{
    Task<SearchQuery> CreateAsync(SearchQuery query, CancellationToken ct = default);
    Task UpdateAsync(SearchQuery query, CancellationToken ct = default);
    Task<SearchQuery?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<SearchHistoryDto>> GetHistoryAsync(int page, int pageSize, CancellationToken ct = default);
}
