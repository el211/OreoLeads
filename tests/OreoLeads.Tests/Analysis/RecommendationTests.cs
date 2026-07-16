using FluentAssertions;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Analysis;

namespace OreoLeads.Tests.Analysis;

public class RecommendationTests
{
    private static WebsiteAnalysis Base() => new()
    {
        LeadId = Guid.NewGuid(),
        Url = "https://example.com",
        HasContactForm = true,
        HasBookingSystem = false,
        HasQuoteForm = false,
        HasViewport = true,
        UsesHttps = true,
        CertificateValid = true,
        HasPrivacyPolicy = true,
        HasLegalNotice = true,
        MetaDescription = "desc",
        ResponseTimeMs = 800,
    };

    [Fact]
    public void Recommendations_Restaurant_IncludesReservation()
    {
        var services = BusinessRecommendationService.GetOreoServices(Base(), "Restaurant");
        services.Should().Contain(s => s.Contains("Réservation") || s.Contains("réservation"));
    }

    [Fact]
    public void Recommendations_Garage_IncludesDevis()
    {
        var services = BusinessRecommendationService.GetOreoServices(Base(), "Garage automobile");
        services.Should().Contain(s => s.Contains("devis") || s.Contains("Devis"));
    }

    [Fact]
    public void Recommendations_Immobilier_IncludesCRM()
    {
        var services = BusinessRecommendationService.GetOreoServices(Base(), "Agence immobilière");
        services.Should().Contain(s => s.Contains("CRM") || s.Contains("leads"));
    }

    [Fact]
    public void Recommendations_Artisan_IncludesDevis()
    {
        var services = BusinessRecommendationService.GetOreoServices(Base(), "Plombier artisan");
        services.Should().Contain(s => s.Contains("devis") || s.Contains("Devis"));
    }

    [Fact]
    public void Recommendations_Medecin_IncludesRdv()
    {
        var services = BusinessRecommendationService.GetOreoServices(Base(), "Cabinet médecin");
        services.Should().Contain(s => s.Contains("rendez-vous") || s.Contains("Doctolib"));
    }

    [Fact]
    public void Recommendations_UnknownIndustry_ReturnsPMEDefaults()
    {
        var services = BusinessRecommendationService.GetOreoServices(Base(), "Entreprise inconnue");
        services.Should().NotBeEmpty();
        services.Should().Contain(s => s.Contains("CRM") || s.Contains("logiciel") || s.Contains("Logiciel"));
    }

    [Fact]
    public void Recommendations_MaxEight()
    {
        var a = Base();
        a.HasViewport = false;
        a.UsesHttps = false;
        a.HasPrivacyPolicy = false;
        a.ResponseTimeMs = 5000;
        var services = BusinessRecommendationService.GetOreoServices(a, "Restaurant");
        services.Count.Should().BeLessThanOrEqualTo(8);
    }

    [Fact]
    public void Recommendations_NoDuplicates()
    {
        var services = BusinessRecommendationService.GetOreoServices(Base(), "Restaurant");
        services.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Recommendations_AddsHttpsService_WhenNoHttps()
    {
        var a = Base();
        a.UsesHttps = false;
        var services = BusinessRecommendationService.GetOreoServices(a, "PME");
        services.Should().Contain(s => s.Contains("HTTPS") || s.Contains("SSL"));
    }

    [Fact]
    public void Recommendations_AddsResponsive_WhenNoViewport()
    {
        var a = Base();
        a.HasViewport = false;
        var services = BusinessRecommendationService.GetOreoServices(a, "PME");
        services.Should().Contain(s => s.Contains("responsive") || s.Contains("mobile"));
    }
}
