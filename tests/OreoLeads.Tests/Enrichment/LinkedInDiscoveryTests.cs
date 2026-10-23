using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Enrichment;

namespace OreoLeads.Tests.Enrichment;

public class LinkedInDiscoveryTests
{
    [Theory]
    [InlineData("https://fr.linkedin.com/in/jane-doe/?trk=public", "https://www.linkedin.com/in/jane-doe")]
    [InlineData("https://linkedin.com/company/acme", "https://www.linkedin.com/company/acme")]
    [InlineData("https://linkedin.com.evil.example/in/jane", null)]
    [InlineData("https://notlinkedin.com/in/jane", null)]
    [InlineData("https://linkedin.com@evil.example/in/jane", null)]
    [InlineData("https://linkedin.com/jobs/view/123", null)]
    [InlineData("https://linkedin.com/in/jane/posts", null)]
    [InlineData("http://linkedin.com/in/jane", null)]
    public void OnlyCanonicalLinkedInCompanyAndPeopleUrlsAreAccepted(string input, string? expected)
        => Assert.Equal(expected, LinkedInDiscoveryService.CanonicalProfileUrl(input));

    [Fact]
    public async Task SearchesCompanyAndPeople_DeduplicatesAndFiltersUnrelatedResults()
    {
        var brave = new FakeBraveClient(
            new("https://fr.linkedin.com/in/jane/?trk=one", "Jane — Acme", "Directrice chez Acme, Strasbourg"),
            new("https://www.linkedin.com/in/jane", "Jane — Acme", "Duplicate"),
            new("https://linkedin.com/company/acme", "Acme", "Strasbourg"),
            new("https://linkedin.com/in/other", "Other", "Unrelated company"),
            new("https://evil.example/company/acme", "Acme", "Not LinkedIn"));
        var result = await new LinkedInDiscoveryService(brave, NullLogger<LinkedInDiscoveryService>.Instance)
            .DiscoverAsync(new Lead { CompanyName = "Acme", City = "Strasbourg" });
        Assert.Equal(3, brave.CallCount);
        Assert.Equal(3, result.QueriesUsed);
        Assert.Equal("completed", result.Status);
        Assert.Equal(2, result.Profiles.Count);
        Assert.Contains(result.Profiles, p => p.Kind == "company");
        Assert.Contains(result.Profiles, p => p.Kind == "person");
    }

    [Fact]
    public async Task MissingSearchKeyIsExplicitAndMakesNoRequests()
    {
        var brave = new FakeBraveClient { IsConfigured = false };
        var result = await new LinkedInDiscoveryService(brave, NullLogger<LinkedInDiscoveryService>.Instance)
            .DiscoverAsync(new Lead { CompanyName = "Acme" });
        Assert.Equal("notConfigured", result.Status);
        Assert.Equal(0, brave.CallCount);
    }

    [Fact]
    public void UpdatingResearchPreservesWebsiteInspectionAndReplacesOldSearch()
    {
        const string original = """[{"sourceUrl":"https://example.org","phones":["+33612345678"]},{"kind":"linkedinSearch","status":"old"}]""";
        var updated = LinkedInDiscoveryService.Attach(original,
            new LinkedInResearch("linkedinSearch", "completed", DateTime.UtcNow, 3, [], null));
        using var json = JsonDocument.Parse(updated);
        Assert.Equal(2, json.RootElement.GetArrayLength());
        Assert.Equal("https://example.org", json.RootElement[0].GetProperty("sourceUrl").GetString());
        Assert.Equal("completed", json.RootElement[1].GetProperty("status").GetString());
    }

    [Fact]
    public async Task ProviderFailuresAreVisibleWithoutFailingTheEntireEnrichment()
    {
        var result = await new LinkedInDiscoveryService(new FailedSearch(), NullLogger<LinkedInDiscoveryService>.Instance)
            .DiscoverAsync(new Lead { CompanyName = "Acme" });
        Assert.Equal("failed", result.Status);
        Assert.NotNull(result.Error);
        Assert.Empty(result.Profiles);
    }

    private sealed class FailedSearch : IBraveSearchClient
    {
        public bool IsConfigured => true;
        public Task<List<WebSearchResult>> SearchAsync(string query, CancellationToken ct = default)
            => throw new HttpRequestException("Unavailable");
    }
}
