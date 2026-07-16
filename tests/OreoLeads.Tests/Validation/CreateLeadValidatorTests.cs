using FluentAssertions;
using OreoLeads.Application.Features.Leads.DTOs;
using OreoLeads.Application.Features.Leads.Validators;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Tests.Validation;

public class CreateLeadValidatorTests
{
    private readonly CreateLeadValidator _validator = new();

    [Fact]
    public async Task Valid_lead_should_pass()
    {
        var dto = new CreateLeadDto
        {
            CompanyName = "Oreo Studios",
            Email = "contact@oreo.fr",
            Phone = "0612345678",
            Website = "https://oreo.fr",
            Score = 75
        };

        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_company_name_should_fail()
    {
        var dto = new CreateLeadDto { CompanyName = "" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyName");
    }

    [Fact]
    public async Task Company_name_too_long_should_fail()
    {
        var dto = new CreateLeadDto { CompanyName = new string('A', 201) };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyName");
    }

    [Fact]
    public async Task Invalid_email_should_fail()
    {
        var dto = new CreateLeadDto
        {
            CompanyName = "Test",
            Email = "not-an-email"
        };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Empty_email_should_pass()
    {
        var dto = new CreateLeadDto { CompanyName = "Test", Email = null };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Invalid_website_should_fail()
    {
        var dto = new CreateLeadDto
        {
            CompanyName = "Test",
            Website = "not-a-url"
        };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Website");
    }

    [Fact]
    public async Task Valid_https_website_should_pass()
    {
        var dto = new CreateLeadDto
        {
            CompanyName = "Test",
            Website = "https://www.example.com"
        };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(200)]
    public async Task Score_out_of_range_should_fail(int score)
    {
        var dto = new CreateLeadDto { CompanyName = "Test", Score = score };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Score");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task Score_in_range_should_pass(int score)
    {
        var dto = new CreateLeadDto { CompanyName = "Test", Score = score };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task All_statuses_should_be_valid()
    {
        foreach (var status in Enum.GetValues<LeadStatus>())
        {
            var dto = new CreateLeadDto { CompanyName = "Test", Status = status };
            var result = await _validator.ValidateAsync(dto);
            result.IsValid.Should().BeTrue($"Status {status} should be valid");
        }
    }
}
