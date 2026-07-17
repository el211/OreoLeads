using FluentAssertions;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Tests.Automation;

public class AutomationVariableTests
{
    [Fact]
    public void SetVariable_ThenGet_ReturnsValue()
    {
        var ctx = AutomationTestHelpers.CreateContext();
        ctx.SetVariable("key", "value");

        ctx.GetVariable("key").Should().Be("value");
    }

    [Fact]
    public void InterpolateString_LeadVariable_ReturnsInterpolated()
    {
        var ctx = AutomationTestHelpers.CreateContext();
        ctx.SetVariable("Lead.CompanyName", "ACME");

        var result = ctx.InterpolateString("Hello {{Lead.CompanyName}}!");

        result.Should().Be("Hello ACME!");
    }

    [Fact]
    public void InterpolateString_DateNow_ReturnsDate()
    {
        var ctx = AutomationTestHelpers.CreateContext();

        var result = ctx.InterpolateString("Today is {{Date.Now}}");

        result.Should().StartWith("Today is ");
        result.Should().NotContain("{{Date.Now}}");
    }

    [Fact]
    public void InterpolateString_UnknownVariable_LeavesPlaceholder()
    {
        var ctx = AutomationTestHelpers.CreateContext();

        var result = ctx.InterpolateString("Hello {{Unknown.Var}}!");

        result.Should().Be("Hello {{Unknown.Var}}!");
    }

    [Fact]
    public void SetVariable_OverwritesExisting()
    {
        var ctx = AutomationTestHelpers.CreateContext();
        ctx.SetVariable("key", "first");
        ctx.SetVariable("key", "second");

        ctx.GetVariable("key").Should().Be("second");
    }

    [Fact]
    public void InterpolateString_MultipleVariables_ReturnsAllInterpolated()
    {
        var ctx = AutomationTestHelpers.CreateContext();
        ctx.SetVariable("Name", "John");
        ctx.SetVariable("Company", "ACME");

        var result = ctx.InterpolateString("{{Name}} from {{Company}}");

        result.Should().Be("John from ACME");
    }
}
