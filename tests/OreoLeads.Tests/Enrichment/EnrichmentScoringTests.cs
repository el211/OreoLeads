using FluentAssertions;
using OreoLeads.Infrastructure.Enrichment;

namespace OreoLeads.Tests.Enrichment;

public class EnrichmentScoringTests
{
    private static readonly EnrichmentScoring.CompanyIdentity Boulangerie = new(
        DisplayName: "Boulangerie du Coin",
        City: "Lyon",
        PostalCode: "69001",
        Phone: "04 78 12 34 56",
        Siren: "111222333",
        Siret: "11122233300012");

    [Fact]
    public void SirenInPage_FloorsScoreVeryHigh()
    {
        var html = "<html><body>Boulangerie du Coin - SIREN 111 222 333 - Lyon</body></html>";
        var result = EnrichmentScoring.ScoreCandidate(Boulangerie, "https://boulangerie-du-coin.fr", html);

        result.Score.Should().BeGreaterThanOrEqualTo(0.95);
        result.Signals.Should().Contain("siren");
    }

    [Fact]
    public void NameCityAndPhone_ProduceStrongScore()
    {
        var html = "<h1>Boulangerie du Coin</h1><p>Lyon — 04.78.12.34.56</p>";
        var result = EnrichmentScoring.ScoreCandidate(Boulangerie, "https://autresite.fr", html);

        result.Signals.Should().Contain("name").And.Contain("city").And.Contain("phone");
        result.Score.Should().BeGreaterThanOrEqualTo(0.7);
    }

    [Fact]
    public void UnrelatedPage_ScoresLow()
    {
        var html = "<html><body>Vente de voitures d'occasion à Bordeaux</body></html>";
        var result = EnrichmentScoring.ScoreCandidate(Boulangerie, "https://voitures-bordeaux.fr", html);

        result.Score.Should().BeLessThan(0.4);
    }

    [Fact]
    public void DomainMatchingName_AddsDomainSignal()
    {
        var html = "<h1>Boulangerie du Coin</h1>";
        var result = EnrichmentScoring.ScoreCandidate(Boulangerie, "https://boulangerieducoin.fr", html);
        result.Signals.Should().Contain("domain");
    }

    [Theory]
    [InlineData("https://www.pagesjaunes.fr/pro/123", "pagesjaunes.fr", true)]
    [InlineData("https://m.facebook.com/monresto", "facebook.com", true)]
    [InlineData("https://boulangerie-du-coin.fr", "pagesjaunes.fr", false)]
    public void IsBlacklisted_MatchesRegistrableDomain(string url, string blacklistEntry, bool expected)
    {
        EnrichmentScoring.IsBlacklisted(url, [blacklistEntry]).Should().Be(expected);
    }

    [Fact]
    public void GetRegistrableDomain_StripsSubdomains()
    {
        EnrichmentScoring.GetRegistrableDomain("https://www.sub.example.fr/path").Should().Be("example.fr");
    }
}
