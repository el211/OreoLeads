using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Search.DTOs;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Services;

namespace OreoLeads.Tests.Search;

public class SearchServicePaginationTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, tenant: null);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ImportAsync_BulkPreload_DetectsDuplicatesWithoutNPlusOne()
    {
        // Arrange
        await using var db = CreateDbContext();

        // Seed existing lead
        var existingLead = new OreoLeads.Domain.Entities.Lead
        {
            CompanyName = "Existing Corp",
            Siren = "123456789",
        };
        db.Leads.Add(existingLead);
        await db.SaveChangesAsync();

        var repo = new FakeSearchRepository();
        var svc = new SearchService([], repo, db, NullLogger<SearchService>.Instance);

        var request = new SearchImportRequestDto
        {
            Companies =
            [
                new() { CompanyName = "Existing Corp", Siren = "123456789" },  // duplicate
                new() { CompanyName = "New Corp", Siren = "987654321" },        // new
                new() { CompanyName = "Another New", Siren = "555555555" },     // new
            ]
        };

        // Act
        var result = await svc.ImportAsync(request);

        // Assert — "Existing Corp" matches by Siren but has no new data to enrich → duplicate
        result.NewLeads.Should().Be(2);
        result.Duplicates.Should().Be(1, because: "existing corp matches by SIREN but has no new fields to enrich");
        result.UpdatedLeads.Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_InBatchDuplicate_CountedOnce()
    {
        await using var db = CreateDbContext();
        var repo = new FakeSearchRepository();
        var svc = new SearchService([], repo, db, NullLogger<SearchService>.Instance);

        var request = new SearchImportRequestDto
        {
            Companies =
            [
                new() { CompanyName = "Corp A", Siren = "111111111" },
                new() { CompanyName = "Corp A", Siren = "111111111" },  // same siren — in-batch dup
            ]
        };

        var result = await svc.ImportAsync(request);

        result.NewLeads.Should().Be(1);
        result.Duplicates.Should().Be(1);
    }

    [Fact]
    public static void EnrichLead_FillsEmptyFields_ReturnsTrue()
    {
        var lead = new OreoLeads.Domain.Entities.Lead
        {
            CompanyName = "Test",
            Siren = "111",
        };

        var company = new CompanySearchResultDto
        {
            CompanyName = "Test",
            Siren = "111",
            NafCode = "6201Z",
            City = "Paris",
            Website = "https://test.fr",
        };

        var enriched = SearchService.EnrichLead(lead, company);

        enriched.Should().BeTrue();
        lead.NafCode.Should().Be("6201Z");
        lead.City.Should().Be("Paris");
    }

    [Fact]
    public static void EnrichLead_DoesNotOverwriteExistingData()
    {
        var lead = new OreoLeads.Domain.Entities.Lead
        {
            CompanyName = "Test",
            Siren = "111",
            NafCode = "EXISTING",
            City = "Lyon",
        };

        var company = new CompanySearchResultDto
        {
            CompanyName = "Test",
            NafCode = "NEW_NAF",
            City = "Paris",
        };

        SearchService.EnrichLead(lead, company);

        lead.NafCode.Should().Be("EXISTING");
        lead.City.Should().Be("Lyon");
    }

    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeSearchRepository : ISearchRepository
    {
        public Task<OreoLeads.Domain.Entities.SearchQuery> CreateAsync(OreoLeads.Domain.Entities.SearchQuery query, CancellationToken ct = default)
            => Task.FromResult(query);
        public Task<OreoLeads.Domain.Entities.SearchQuery?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<OreoLeads.Domain.Entities.SearchQuery?>(null);
        public Task UpdateAsync(OreoLeads.Domain.Entities.SearchQuery query, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task<OreoLeads.Application.Common.Models.PagedResult<OreoLeads.Application.Features.Search.DTOs.SearchHistoryDto>> GetHistoryAsync(int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new OreoLeads.Application.Common.Models.PagedResult<OreoLeads.Application.Features.Search.DTOs.SearchHistoryDto>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
            });
    }
}
