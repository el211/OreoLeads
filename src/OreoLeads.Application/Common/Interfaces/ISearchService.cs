using OreoLeads.Application.Features.Search.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface ISearchService
{
    Task<CompanySearchResponseDto> SearchAsync(CompanySearchRequestDto request, CancellationToken ct = default);
    Task<SearchImportResultDto> ImportAsync(SearchImportRequestDto request, CancellationToken ct = default);
}
