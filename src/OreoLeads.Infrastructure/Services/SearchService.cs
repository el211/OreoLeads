using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Search.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly IEnumerable<ILeadSource> _sources;
    private readonly ISearchRepository _searchRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        IEnumerable<ILeadSource> sources,
        ISearchRepository searchRepository,
        ApplicationDbContext context,
        ILogger<SearchService> logger)
    {
        _sources = sources;
        _searchRepository = searchRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<CompanySearchResponseDto> SearchAsync(
        CompanySearchRequestDto request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var source = _sources.First(); // architecture multi-sources : le premier par ordre de priorité
        var (results, totalFound) = await source.SearchAsync(request, 1, ct);

        // Détection des doublons dans la BDD existante
        foreach (var result in results)
        {
            var duplicate = await FindExistingLeadAsync(result, ct);
            if (duplicate != null)
            {
                result.AlreadyExists = true;
                result.ExistingLeadId = duplicate.Id;
            }
        }

        sw.Stop();

        var searchQuery = new SearchQuery
        {
            Keywords = request.Keywords,
            Region = request.Region,
            Department = request.Department,
            City = request.City,
            PostalCode = request.PostalCode,
            Industry = request.Industry,
            NafCode = request.NafCode,
            ActiveOnly = request.ActiveOnly,
            MaxResults = request.MaxResults,
            Provider = source.ProviderName,
            DurationMs = (int)sw.ElapsedMilliseconds,
            TotalFound = totalFound,
            Status = "Searched",
        };

        var saved = await _searchRepository.CreateAsync(searchQuery, ct);

        return new CompanySearchResponseDto
        {
            SearchId = saved.Id,
            TotalFound = totalFound,
            Results = results,
            Provider = source.ProviderName,
            DurationMs = (int)sw.ElapsedMilliseconds,
        };
    }

    public async Task<SearchImportResultDto> ImportAsync(
        SearchImportRequestDto request, CancellationToken ct = default)
    {
        var result = new SearchImportResultDto();

        foreach (var company in request.Companies)
        {
            try
            {
                var existing = await FindExistingLeadAsync(company, ct);

                if (existing != null)
                {
                    if (EnrichLead(existing, company))
                    {
                        existing.SetUpdatedAt();
                        result.UpdatedLeads++;
                    }
                    else
                    {
                        result.Duplicates++;
                    }
                }
                else
                {
                    var lead = new Lead
                    {
                        CompanyName = company.CompanyName,
                        Industry = company.Industry,
                        Siren = company.Siren,
                        Siret = company.Siret,
                        NafCode = company.NafCode,
                        Address = company.Address,
                        PostalCode = company.PostalCode,
                        City = company.City != null
                            ? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(company.City.ToLower())
                            : null,
                        Department = company.Department,
                        Region = company.Region,
                        Phone = company.Phone,
                        Email = company.Email,
                        Website = company.Website,
                        Latitude = company.Latitude,
                        Longitude = company.Longitude,
                        EmployeeCount = company.EmployeeCount,
                        Country = "France",
                        Status = LeadStatus.New,
                        Priority = LeadPriority.Medium,
                    };

                    _context.Leads.Add(lead);
                    result.NewLeadIds.Add(lead.Id);
                    result.NewLeads++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erreur lors de l'import de {CompanyName}", company.CompanyName);
                result.Errors++;
            }
        }

        await _context.SaveChangesAsync(ct);

        if (request.SearchId.HasValue)
        {
            var searchQuery = await _searchRepository.GetByIdAsync(request.SearchId.Value, ct);
            if (searchQuery != null)
            {
                searchQuery.NewLeads = result.NewLeads;
                searchQuery.UpdatedLeads = result.UpdatedLeads;
                searchQuery.Duplicates = result.Duplicates;
                searchQuery.Errors = result.Errors;
                searchQuery.Status = "Imported";
                await _searchRepository.UpdateAsync(searchQuery, ct);
            }
        }

        return result;
    }

    private async Task<Lead?> FindExistingLeadAsync(CompanySearchResultDto company, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(company.Siren))
        {
            var bySiren = await _context.Leads
                .FirstOrDefaultAsync(l => l.Siren == company.Siren, ct);
            if (bySiren != null) return bySiren;
        }

        if (!string.IsNullOrWhiteSpace(company.Siret))
        {
            var bySiret = await _context.Leads
                .FirstOrDefaultAsync(l => l.Siret == company.Siret, ct);
            if (bySiret != null) return bySiret;
        }

        // Fallback : correspondance nom + code postal
        if (!string.IsNullOrWhiteSpace(company.CompanyName) && !string.IsNullOrWhiteSpace(company.PostalCode))
        {
            return await _context.Leads.FirstOrDefaultAsync(l =>
                l.CompanyName.ToLower() == company.CompanyName.ToLower() &&
                l.PostalCode == company.PostalCode, ct);
        }

        return null;
    }

    /// <summary>
    /// Enrichit les champs vides d'un Lead existant sans écraser les données manuelles.
    /// Retourne true si au moins un champ a été enrichi.
    /// </summary>
    internal static bool EnrichLead(Lead existing, CompanySearchResultDto company)
    {
        var enriched = false;

        if (string.IsNullOrEmpty(existing.Siren) && !string.IsNullOrEmpty(company.Siren))
        { existing.Siren = company.Siren; enriched = true; }

        if (string.IsNullOrEmpty(existing.Siret) && !string.IsNullOrEmpty(company.Siret))
        { existing.Siret = company.Siret; enriched = true; }

        if (string.IsNullOrEmpty(existing.NafCode) && !string.IsNullOrEmpty(company.NafCode))
        { existing.NafCode = company.NafCode; enriched = true; }

        if (string.IsNullOrEmpty(existing.Industry) && !string.IsNullOrEmpty(company.Industry))
        { existing.Industry = company.Industry; enriched = true; }

        if (string.IsNullOrEmpty(existing.Address) && !string.IsNullOrEmpty(company.Address))
        { existing.Address = company.Address; enriched = true; }

        if (string.IsNullOrEmpty(existing.PostalCode) && !string.IsNullOrEmpty(company.PostalCode))
        { existing.PostalCode = company.PostalCode; enriched = true; }

        if (string.IsNullOrEmpty(existing.City) && !string.IsNullOrEmpty(company.City))
        { existing.City = company.City; enriched = true; }

        if (string.IsNullOrEmpty(existing.Department) && !string.IsNullOrEmpty(company.Department))
        { existing.Department = company.Department; enriched = true; }

        if (string.IsNullOrEmpty(existing.Region) && !string.IsNullOrEmpty(company.Region))
        { existing.Region = company.Region; enriched = true; }

        if (existing.Latitude == null && company.Latitude.HasValue)
        { existing.Latitude = company.Latitude; enriched = true; }

        if (existing.Longitude == null && company.Longitude.HasValue)
        { existing.Longitude = company.Longitude; enriched = true; }

        if (existing.EmployeeCount == null && company.EmployeeCount.HasValue)
        { existing.EmployeeCount = company.EmployeeCount; enriched = true; }

        return enriched;
    }
}
