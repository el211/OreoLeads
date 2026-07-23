using FluentAssertions;
using OreoLeads.Application.Features.Leads.DTOs;
using OreoLeads.Application.Features.Leads.Validators;

namespace OreoLeads.Tests.Validation;

public class UpdateLeadValidatorTests
{
    private readonly UpdateLeadValidator _validator = new();

    [Fact]
    public async Task Valid_update_dto_should_pass()
    {
        var dto = new UpdateLeadDto
        {
            CompanyName = "Oreo Studios",
            Email = "contact@oreo.fr",
            Score = 80
        };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_company_name_should_fail()
    {
        var dto = new UpdateLeadDto { CompanyName = "" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyName");
    }

    [Fact]
    public async Task Invalid_email_should_fail()
    {
        var dto = new UpdateLeadDto { CompanyName = "Test", Email = "bad" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Score_above_100_should_fail()
    {
        var dto = new UpdateLeadDto { CompanyName = "Test", Score = 150 };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("cherrier-kuhn-magret-rosheim.notaires.fr")]
    [InlineData("https://oreostudios.fr")]
    [InlineData("http://exemple.fr/contact")]
    [InlineData("www.exemple.fr")]
    public async Task Website_bare_domain_or_full_url_should_pass(string website)
    {
        var dto = new UpdateLeadDto { CompanyName = "Test", Website = website, Score = 0 };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("pas une url")]
    [InlineData("http://")]
    public async Task Website_garbage_should_fail(string website)
    {
        var dto = new UpdateLeadDto { CompanyName = "Test", Website = website, Score = 0 };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();
    }
}
