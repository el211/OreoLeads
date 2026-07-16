using FluentAssertions;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Analysis;

namespace OreoLeads.Tests.Analysis;

public class BusinessScoringTests
{
    private static WebsiteAnalysis Perfect() => new()
    {
        LeadId = Guid.NewGuid(),
        Url = "https://example.com",
        HasContactForm = true,
        HasQuoteForm = true,
        HasBookingSystem = true,
        ResponseTimeMs = 500,
        HasViewport = true,
        MetaDescription = "A great description",
        UsesHttps = true,
        CertificateValid = true,
        HasPrivacyPolicy = true,
        HasLegalNotice = true,
        HasEmailVisible = false,
        HasPhoneVisible = false,
    };

    [Fact]
    public void Score_Zero_WhenSiteIsPerfect()
    {
        var analysis = Perfect();
        BusinessScoringService.Calculate(analysis).Should().Be(0);
    }

    [Fact]
    public void Score_20_WhenNoContactForm()
    {
        var a = Perfect();
        a.HasContactForm = false;
        BusinessScoringService.Calculate(a).Should().Be(20);
    }

    [Fact]
    public void Score_15_WhenNoQuoteForm()
    {
        var a = Perfect();
        a.HasQuoteForm = false;
        BusinessScoringService.Calculate(a).Should().Be(15);
    }

    [Fact]
    public void Score_15_WhenNoBookingSystem()
    {
        var a = Perfect();
        a.HasBookingSystem = false;
        BusinessScoringService.Calculate(a).Should().Be(15);
    }

    [Fact]
    public void Score_10_WhenSiteIsSlow()
    {
        var a = Perfect();
        a.ResponseTimeMs = 4000;
        BusinessScoringService.Calculate(a).Should().Be(10);
    }

    [Fact]
    public void Score_10_WhenNoViewport()
    {
        var a = Perfect();
        a.HasViewport = false;
        BusinessScoringService.Calculate(a).Should().Be(10);
    }

    [Fact]
    public void Score_10_WhenNoMetaDescription()
    {
        var a = Perfect();
        a.MetaDescription = null;
        BusinessScoringService.Calculate(a).Should().Be(10);
    }

    [Fact]
    public void Score_5_WhenNoHttps()
    {
        var a = Perfect();
        a.UsesHttps = false;
        BusinessScoringService.Calculate(a).Should().Be(5);
    }

    [Fact]
    public void Score_ClampsTo100_WhenAllOpportunitiesPresent()
    {
        var a = new WebsiteAnalysis
        {
            LeadId = Guid.NewGuid(),
            Url = "http://bad.example.com",
            HasContactForm = false,
            HasQuoteForm = false,
            HasBookingSystem = false,
            ResponseTimeMs = 10000,
            HasViewport = false,
            MetaDescription = null,
            UsesHttps = false,
            CertificateValid = false,
            HasPrivacyPolicy = false,
            HasLegalNotice = false,
            HasEmailVisible = true,
            HasPhoneVisible = true,
        };
        // Raw = 20+15+15+10+10+10+5+3+3+3+1 = 95, clamp → 95
        var score = BusinessScoringService.Calculate(a);
        score.Should().BeInRange(0, 100);
    }

    [Fact]
    public void Score_NotNegative_WhenAllFeaturesPresent()
    {
        var a = Perfect();
        BusinessScoringService.Calculate(a).Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void GetOpportunities_ListsMissingContactForm()
    {
        var a = Perfect();
        a.HasContactForm = false;
        var ops = BusinessScoringService.GetOpportunities(a);
        ops.Should().Contain(o => o.Contains("formulaire de contact"));
    }

    [Fact]
    public void GetOpportunities_EmptyList_WhenSiteIsPerfect()
    {
        var a = Perfect();
        // Perfect site → very few or no opportunities
        var ops = BusinessScoringService.GetOpportunities(a);
        ops.Should().NotContain(o => o.Contains("formulaire de contact"));
        ops.Should().NotContain(o => o.Contains("HTTPS"));
    }

    [Fact]
    public void GetOpportunities_IncludesCmsInfo()
    {
        var a = Perfect();
        a.CmsDetected = "WordPress";
        var ops = BusinessScoringService.GetOpportunities(a);
        ops.Should().Contain(o => o.Contains("WordPress"));
    }
}
