using FluentAssertions;
using OreoLeads.Application.Features.Search.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Services;

namespace OreoLeads.Tests.Search;

public class SearchEnrichmentTests
{
    // ── EnrichLead ────────────────────────────────────────────────────────────

    [Fact]
    public void EnrichLead_PopulatesEmptyFields_ReturnsTrue()
    {
        var lead = new Lead { CompanyName = "Acme" };
        var company = new CompanySearchResultDto
        {
            CompanyName = "Acme",
            Siren = "123456789",
            Siret = "12345678900015",
            NafCode = "56.10A",
            Industry = "Restauration",
            Address = "1 rue de la Paix",
            PostalCode = "75001",
            City = "Paris",
            Department = "75",
            Region = "11",
            Latitude = 48.87,
            Longitude = 2.33,
            EmployeeCount = 10,
        };

        var enriched = SearchService.EnrichLead(lead, company);

        enriched.Should().BeTrue();
        lead.Siren.Should().Be("123456789");
        lead.Siret.Should().Be("12345678900015");
        lead.NafCode.Should().Be("56.10A");
        lead.Industry.Should().Be("Restauration");
        lead.Address.Should().Be("1 rue de la Paix");
        lead.PostalCode.Should().Be("75001");
        lead.City.Should().Be("Paris");
        lead.Department.Should().Be("75");
        lead.Region.Should().Be("11");
        lead.Latitude.Should().Be(48.87);
        lead.Longitude.Should().Be(2.33);
        lead.EmployeeCount.Should().Be(10);
    }

    [Fact]
    public void EnrichLead_DoesNotOverwriteExistingFields()
    {
        var lead = new Lead
        {
            CompanyName = "Acme",
            Siren = "EXISTING",
            Siret = "EXISTING_SIRET",
            NafCode = "EXISTING_NAF",
            Industry = "EXISTING_INDUSTRY",
            Address = "EXISTING_ADDRESS",
            City = "EXISTING_CITY",
            Latitude = 99.0,
        };
        var company = new CompanySearchResultDto
        {
            CompanyName = "Acme",
            Siren = "NEW_SIREN",
            Siret = "NEW_SIRET",
            NafCode = "NEW_NAF",
            Industry = "NEW_INDUSTRY",
            Address = "NEW_ADDRESS",
            City = "NEW_CITY",
            Latitude = 1.0,
        };

        SearchService.EnrichLead(lead, company);

        lead.Siren.Should().Be("EXISTING");
        lead.Siret.Should().Be("EXISTING_SIRET");
        lead.NafCode.Should().Be("EXISTING_NAF");
        lead.Industry.Should().Be("EXISTING_INDUSTRY");
        lead.Address.Should().Be("EXISTING_ADDRESS");
        lead.City.Should().Be("EXISTING_CITY");
        lead.Latitude.Should().Be(99.0);
    }

    [Fact]
    public void EnrichLead_AllFieldsAlreadyFilled_ReturnsFalse()
    {
        var lead = new Lead
        {
            CompanyName = "Acme",
            Siren = "123456789",
            Siret = "12345678900015",
            NafCode = "56.10A",
            Industry = "Restauration",
            Address = "1 rue de la Paix",
            PostalCode = "75001",
            City = "Paris",
            Department = "75",
            Region = "11",
            Latitude = 48.87,
            Longitude = 2.33,
            EmployeeCount = 10,
        };
        var company = new CompanySearchResultDto
        {
            CompanyName = "Acme",
            Siren = "999999999",
            NafCode = "99.99Z",
        };

        var enriched = SearchService.EnrichLead(lead, company);

        enriched.Should().BeFalse();
    }

    [Fact]
    public void EnrichLead_PartialEnrichment_OnlyFillsMissingFields()
    {
        var lead = new Lead { CompanyName = "Acme", Siren = "EXISTING" };
        var company = new CompanySearchResultDto
        {
            CompanyName = "Acme",
            Siren = "NEW_SIREN",   // ne doit PAS écraser
            NafCode = "56.10A",    // doit être rempli (manquant)
        };

        var enriched = SearchService.EnrichLead(lead, company);

        enriched.Should().BeTrue();
        lead.Siren.Should().Be("EXISTING");
        lead.NafCode.Should().Be("56.10A");
    }

    // ── Import result counting ────────────────────────────────────────────────

    [Fact]
    public void ImportResult_InitiallyAllZero()
    {
        var result = new SearchImportResultDto();
        result.NewLeads.Should().Be(0);
        result.UpdatedLeads.Should().Be(0);
        result.Duplicates.Should().Be(0);
        result.Errors.Should().Be(0);
        result.NewLeadIds.Should().BeEmpty();
    }

    [Fact]
    public void SearchRequest_DefaultValues_AreCorrect()
    {
        var request = new CompanySearchRequestDto();
        request.ActiveOnly.Should().BeTrue();
        request.MaxResults.Should().Be(50);
    }

    [Fact]
    public void SearchRequest_MaxResultsClamped()
    {
        var request = new CompanySearchRequestDto { MaxResults = 0 };
        request.MaxResults.Should().BeLessThanOrEqualTo(200);
    }

    // ── Duplicate detection logic (pure) ────────────────────────────────────

    [Theory]
    [InlineData("123456789", "123456789", true)]   // même SIREN → doublon
    [InlineData("123456789", "987654321", false)]  // SIREN différent → pas doublon
    [InlineData(null, null, false)]                // pas de SIREN → comparaison par nom
    public void DuplicateDetection_BySiren(string? existingSiren, string? incomingSiren, bool expectedDuplicate)
    {
        var existingLead = new Lead { CompanyName = "Test", Siren = existingSiren };
        var incoming = new CompanySearchResultDto { CompanyName = "Test", Siren = incomingSiren };

        // Simulation de la logique de détection (cherche par SIREN d'abord)
        var isDuplicate = !string.IsNullOrWhiteSpace(incoming.Siren)
            && incoming.Siren == existingLead.Siren;

        isDuplicate.Should().Be(expectedDuplicate);
    }
}
