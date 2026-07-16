using System.Diagnostics;
using System.Globalization;
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

    private const int PageSize = 20;

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

        var source = _sources.First();
        var allResults = new List<CompanySearchResultDto>();
        var totalFound = 0;
        var page = 1;

        // Paginate until we hit MaxResults or the source runs out
        while (allResults.Count < request.MaxResults)
        {
            var (results, total) = await source.SearchAsync(request, page, ct);
            totalFound = total;

            if (results.Count == 0) break;

            allResults.AddRange(results);
            if (allResults.Count >= request.MaxResults || results.Count < PageSize) break;

            page++;
        }

        // Trim to MaxResults
        if (allResults.Count > request.MaxResults)
            allResults = allResults[..request.MaxResults];

        // Duplicate detection — bulk load existing SIREN/SIRET
        var sirens = allResults
            .Where(r => !string.IsNullOrWhiteSpace(r.Siren))
            .Select(r => r.Siren!)
            .Distinct()
            .ToList();
        var sirets = allResults
            .Where(r => !string.IsNullOrWhiteSpace(r.Siret))
            .Select(r => r.Siret!)
            .Distinct()
            .ToList();

        var existingBySiren = await _context.Leads
            .AsNoTracking()
            .Where(l => sirens.Contains(l.Siren!))
            .ToDictionaryAsync(l => l.Siren!, ct);
        var existingBySiret = await _context.Leads
            .AsNoTracking()
            .Where(l => sirets.Contains(l.Siret!))
            .ToDictionaryAsync(l => l.Siret!, ct);

        foreach (var result in allResults)
        {
            Lead? duplicate = null;
            if (!string.IsNullOrWhiteSpace(result.Siren))
                existingBySiren.TryGetValue(result.Siren, out duplicate);
            if (duplicate == null && !string.IsNullOrWhiteSpace(result.Siret))
                existingBySiret.TryGetValue(result.Siret, out duplicate);

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
            Results = allResults,
            Provider = source.ProviderName,
            DurationMs = (int)sw.ElapsedMilliseconds,
        };
    }

    public async Task<SearchImportResultDto> ImportAsync(
        SearchImportRequestDto request, CancellationToken ct = default)
    {
        var result = new SearchImportResultDto();

        // Bulk preload — avoid N+1 per-company DB lookups
        var sirensToCheck = request.Companies
            .Where(c => !string.IsNullOrWhiteSpace(c.Siren))
            .Select(c => c.Siren!)
            .Distinct()
            .ToList();
        var siretsToCheck = request.Companies
            .Where(c => !string.IsNullOrWhiteSpace(c.Siret))
            .Select(c => c.Siret!)
            .Distinct()
            .ToList();

        var existingBySiren = await _context.Leads
            .Where(l => sirensToCheck.Contains(l.Siren!))
            .ToDictionaryAsync(l => l.Siren!, ct);
        var existingBySiret = await _context.Leads
            .Where(l => siretsToCheck.Contains(l.Siret!))
            .ToDictionaryAsync(l => l.Siret!, ct);

        // In-memory duplicate tracker for the current batch (prevents double-add within same import)
        var sirensSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var siretsSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var company in request.Companies)
        {
            try
            {
                // Find existing lead using preloaded dictionaries
                Lead? existing = null;
                if (!string.IsNullOrWhiteSpace(company.Siren))
                    existingBySiren.TryGetValue(company.Siren, out existing);
                if (existing == null && !string.IsNullOrWhiteSpace(company.Siret))
                    existingBySiret.TryGetValue(company.Siret, out existing);

                // Check in-batch duplicates
                var inBatchDuplicate =
                    (!string.IsNullOrWhiteSpace(company.Siren) && !sirensSeen.Add(company.Siren)) ||
                    (!string.IsNullOrWhiteSpace(company.Siret) && !siretsSeen.Add(company.Siret));

                if (inBatchDuplicate && existing == null)
                {
                    result.Duplicates++;
                    continue;
                }

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
                            ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(company.City.ToLower())
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

                    // Track in-memory to avoid duplicates within the same batch
                    if (!string.IsNullOrWhiteSpace(lead.Siren))
                        existingBySiren[lead.Siren] = lead;
                    if (!string.IsNullOrWhiteSpace(lead.Siret))
                        existingBySiret[lead.Siret] = lead;
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

    private static Lead? FindInDictionaries(
        CompanySearchResultDto company,
        Dictionary<string, Lead> bySiren,
        Dictionary<string, Lead> bySiret)
    {
        if (!string.IsNullOrWhiteSpace(company.Siren) && bySiren.TryGetValue(company.Siren, out var s))
            return s;
        if (!string.IsNullOrWhiteSpace(company.Siret) && bySiret.TryGetValue(company.Siret, out var t))
            return t;
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
