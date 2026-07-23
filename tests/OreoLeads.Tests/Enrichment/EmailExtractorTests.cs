using FluentAssertions;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Enrichment;

namespace OreoLeads.Tests.Enrichment;

public class EmailExtractorTests
{
    [Fact]
    public void Extracts_MailtoLink()
    {
        var emails = EmailExtractor.ExtractAll("<a href=\"mailto:contact@entreprise.fr\">Écrire</a>");
        emails.Should().Contain("contact@entreprise.fr");
    }

    [Fact]
    public void Extracts_PlainVisibleText()
    {
        var emails = EmailExtractor.ExtractAll("<p>Nous écrire : bonjour@monresto.fr</p>");
        emails.Should().Contain("bonjour@monresto.fr");
    }

    [Fact]
    public void Extracts_JsonLdEmail()
    {
        var html = """<script type="application/ld+json">{"@type":"Restaurant","email":"info@resto.fr"}</script>""";
        EmailExtractor.ExtractAll(html).Should().Contain("info@resto.fr");
    }

    [Theory]
    [InlineData("contact [at] entreprise.fr", "contact@entreprise.fr")]
    [InlineData("contact (at) entreprise (dot) fr", "contact@entreprise.fr")]
    [InlineData("contact [arobase] entreprise [point] fr", "contact@entreprise.fr")]
    public void Extracts_ObfuscatedForms(string obfuscated, string expected)
    {
        EmailExtractor.ExtractAll($"<p>{obfuscated}</p>").Should().Contain(expected);
    }

    [Fact]
    public void Extracts_HtmlEntityAt()
    {
        // &#64; = @
        EmailExtractor.ExtractAll("<p>contact&#64;entreprise.fr</p>").Should().Contain("contact@entreprise.fr");
    }

    [Fact]
    public void Filters_NoiseAndAssets()
    {
        var html = "<img src='logo@2x.png'> no-reply@example.com sentry@wixpress.com contact@vrai-site.fr";
        var emails = EmailExtractor.ExtractAll(html);

        emails.Should().Contain("contact@vrai-site.fr");
        emails.Should().NotContain(e => e.Contains("no-reply") || e.Contains("example.com") || e.Contains("wixpress"));
    }

    [Fact]
    public void NoEmailPresent_ReturnsEmpty_NeverGuesses()
    {
        var emails = EmailExtractor.ExtractAll("<p>Aucune adresse ici, juste du texte.</p>");
        emails.Should().BeEmpty();
    }

    [Theory]
    [InlineData("contact@x.fr", DiscoveredEmailKind.Generic)]
    [InlineData("info@x.fr", DiscoveredEmailKind.Generic)]
    [InlineData("jean.dupont@x.fr", DiscoveredEmailKind.Nominative)]
    [InlineData("xyz123@x.fr", DiscoveredEmailKind.Other)]
    public void Classify_DetectsKind(string email, DiscoveredEmailKind expected)
    {
        EmailExtractor.Classify(email).Should().Be(expected);
    }

    [Fact]
    public void IsSameDomain_MatchesSiteHost()
    {
        EmailExtractor.IsSameDomain("contact@entreprise.fr", "https://www.entreprise.fr/contact").Should().BeTrue();
        EmailExtractor.IsSameDomain("contact@gmail.com", "https://www.entreprise.fr").Should().BeFalse();
    }
}
