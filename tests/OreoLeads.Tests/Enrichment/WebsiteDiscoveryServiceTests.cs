using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Enrichment;

namespace OreoLeads.Tests.Enrichment;

public class WebsiteDiscoveryServiceTests
{
    // URLs à IP publique littérale : passent le garde SSRF sans résolution DNS.
    private const string Candidate1 = "http://203.0.113.10/";
    private const string Candidate2 = "http://203.0.113.20/";

    private static Lead SampleLead() => new()
    {
        CompanyName = "Boulangerie du Coin",
        TradeName = "Boulangerie du Coin",
        City = "Lyon",
        PostalCode = "69001",
        Siren = "111222333",
    };

    private static WebsiteDiscoveryService Build(FakeBraveClient brave, FakePageFetcher fetcher)
        => new(brave, fetcher, Options.Create(new EnrichmentSettings()),
               NullLogger<WebsiteDiscoveryService>.Instance);

    [Fact]
    public async Task Blacklisted_Directory_IsBucketedAsExternal_NeverChosen()
    {
        var brave = new FakeBraveClient(
            new WebSearchResult("https://www.pagesjaunes.fr/pro/boulangerie", "PagesJaunes", ""),
            new WebSearchResult("https://facebook.com/boulangerieducoin", "Facebook", ""));
        var fetcher = new FakePageFetcher();

        var result = await Build(brave, fetcher).DiscoverAsync(SampleLead());

        result.ChosenUrl.Should().BeNull();
        result.ExternalProfiles.Should().Contain(p => p.Url.Contains("pagesjaunes"))
            .And.Contain(p => p.Category == "Social");
    }

    [Fact]
    public async Task SirenInPage_AutoAppliesHighConfidence()
    {
        var brave = new FakeBraveClient(new WebSearchResult(Candidate1, "Site", ""));
        var fetcher = new FakePageFetcher().Add(Candidate1,
            "<h1>Boulangerie du Coin</h1><p>Lyon 69001 — SIREN 111 222 333</p>");

        var result = await Build(brave, fetcher).DiscoverAsync(SampleLead());

        result.ChosenUrl.Should().Be(Candidate1);
        result.Confidence.Should().BeGreaterThanOrEqualTo(0.8);
        result.MatchedSignals.Should().Contain("siren");
    }

    [Fact]
    public async Task WeakMatch_BelowThreshold_NotAutoApplied()
    {
        var brave = new FakeBraveClient(new WebSearchResult(Candidate1, "Site", ""));
        // Le nom seul, pas de ville/CP/SIREN → score sous 0.8
        var fetcher = new FakePageFetcher().Add(Candidate1, "<h1>Boulangerie du Coin</h1>");

        var result = await Build(brave, fetcher).DiscoverAsync(SampleLead());

        result.Confidence.Should().BeLessThan(0.8);
        result.Candidates.Should().NotBeEmpty();
    }

    [Fact]
    public async Task NoResults_ReturnsEmpty()
    {
        var result = await Build(new FakeBraveClient(), new FakePageFetcher()).DiscoverAsync(SampleLead());

        result.ChosenUrl.Should().BeNull();
        result.Confidence.Should().Be(0);
        result.Candidates.Should().BeEmpty();
    }
}
