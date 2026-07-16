using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Common.Models;
using OreoLeads.Application.Features.Search.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Repositories;

public class SearchRepository : ISearchRepository
{
    private readonly ApplicationDbContext _context;

    public SearchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SearchQuery> CreateAsync(SearchQuery query, CancellationToken ct = default)
    {
        _context.SearchQueries.Add(query);
        await _context.SaveChangesAsync(ct);
        return query;
    }

    public async Task UpdateAsync(SearchQuery query, CancellationToken ct = default)
    {
        _context.SearchQueries.Update(query);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<SearchQuery?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.SearchQueries.FindAsync([id], ct);

    public async Task<PagedResult<SearchHistoryDto>> GetHistoryAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var totalCount = await _context.SearchQueries.CountAsync(ct);

        var items = await _context.SearchQueries
            .AsNoTracking()
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new SearchHistoryDto
            {
                Id = q.Id,
                Keywords = q.Keywords,
                Region = q.Region,
                Department = q.Department,
                City = q.City,
                PostalCode = q.PostalCode,
                Industry = q.Industry,
                NafCode = q.NafCode,
                ActiveOnly = q.ActiveOnly,
                MaxResults = q.MaxResults,
                Provider = q.Provider,
                DurationMs = q.DurationMs,
                TotalFound = q.TotalFound,
                NewLeads = q.NewLeads,
                UpdatedLeads = q.UpdatedLeads,
                Duplicates = q.Duplicates,
                Errors = q.Errors,
                Status = q.Status,
                CreatedAt = q.CreatedAt,
            })
            .ToListAsync(ct);

        return new PagedResult<SearchHistoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }
}
