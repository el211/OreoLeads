using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Infrastructure.Enrichment;

namespace OreoLeads.Tests.Enrichment;

public class EmailDiscoveryServiceTests
{
    // IP publique littérale → passe le garde SSRF sans DNS.
    private const string Site = "http://203.0.113.10/";

    private static EmailDiscoveryService Build(FakePageFetcher fetcher)
        => new(fetcher, NullLogger<EmailDiscoveryService>.Instance);

    [Fact]
    public async Task FindsEmail_OnHomepage()
    {
        var fetcher = new FakePageFetcher().Add(Site,
            "<html><body><a href=\"mailto:contact@entreprise.fr\">Nous écrire</a></body></html>");

        var result = await Build(fetcher).DiscoverFromWebsiteAsync(Site);

        result.Email.Should().Be("contact@entreprise.fr");
        result.SourceType.Should().Be("Website");
    }

    [Fact]
    public async Task NoEmailAnywhere_ReturnsNull_NeverGuesses()
    {
        var fetcher = new FakePageFetcher().Add(Site, "<html><body>Bienvenue, aucun e-mail ici.</body></html>");

        var result = await Build(fetcher).DiscoverFromWebsiteAsync(Site);

        result.Email.Should().BeNull();
        result.Confidence.Should().Be(0);
    }

    [Fact]
    public async Task NewsletterWidget_WithoutRealAddress_YieldsNoEmail()
    {
        var fetcher = new FakePageFetcher().Add(Site,
            "<form class=\"mc4wp-form\"><input type=\"email\" placeholder=\"Votre email\"><button>S'abonner à la newsletter</button></form>");

        var result = await Build(fetcher).DiscoverFromWebsiteAsync(Site);

        result.Email.Should().BeNull();
    }

    [Fact]
    public async Task ExternalProfiles_Fallback_TagsSource()
    {
        var profileUrl = "http://203.0.113.55/";
        var fetcher = new FakePageFetcher().Add(profileUrl,
            "<p>Contactez-nous : institutpoudre@gmail.com</p>");

        var result = await Build(fetcher).DiscoverFromExternalProfilesAsync(
            new List<ExternalProfile> { new(profileUrl, "Social") });

        result.Email.Should().Be("institutpoudre@gmail.com");
        result.SourceUrl.Should().Be(profileUrl);
    }

    [Fact]
    public async Task ExternalProfiles_NoEmail_ReturnsNull()
    {
        var result = await Build(new FakePageFetcher())
            .DiscoverFromExternalProfilesAsync(new List<ExternalProfile> { new("http://203.0.113.55/", "Social") });

        result.Email.Should().BeNull();
    }
}
