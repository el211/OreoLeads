using System.Text.Json;
using FluentAssertions;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Automation;

namespace OreoLeads.Tests.Automation;

public class AutomationConditionTests
{
    private readonly AutomationConditionEvaluator _evaluator = new();

    [Fact]
    public void Equals_MatchingValue_ReturnsTrue()
    {
        var ctx = AutomationTestHelpers.CreateContext();
        ctx.SetVariable("Status", "Active");

        var json = JsonSerializer.Serialize(new[]
        {
            new { field = "Status", @operator = "Equals", value = "Active" }
        });

        _evaluator.Evaluate(json, ctx).Should().BeTrue();
    }

    [Fact]
    public void Equals_NonMatchingValue_ReturnsFalse()
    {
        var ctx = AutomationTestHelpers.CreateContext();
        ctx.SetVariable("Status", "Active");

        var json = JsonSerializer.Serialize(new[]
        {
            new { field = "Status", @operator = "Equals", value = "Inactive" }
        });

        _evaluator.Evaluate(json, ctx).Should().BeFalse();
    }

    [Fact]
    public void GreaterThan_NumericValue_ReturnsTrue()
    {
        var ctx = AutomationTestHelpers.CreateContext();
        ctx.SetVariable("Score", "85");

        var json = JsonSerializer.Serialize(new[]
        {
            new { field = "Score", @operator = "GreaterThan", value = "50" }
        });

        _evaluator.Evaluate(json, ctx).Should().BeTrue();
    }

    [Fact]
    public void Contains_StringValue_ReturnsTrue()
    {
        var ctx = AutomationTestHelpers.CreateContext();
        ctx.SetVariable("Email", "user@example.com");

        var json = JsonSerializer.Serialize(new[]
        {
            new { field = "Email", @operator = "Contains", value = "example" }
        });

        _evaluator.Evaluate(json, ctx).Should().BeTrue();
    }

    [Fact]
    public void AndCombination_BothTrue_ReturnsTrue()
    {
        var ctx = AutomationTestHelpers.CreateContext();
        ctx.SetVariable("Status", "Active");
        ctx.SetVariable("Score", "85");

        var json = JsonSerializer.Serialize(new
        {
            @operator = "And",
            conditions = new object[]
            {
                new { field = "Status", @operator = "Equals", value = "Active" },
                new { field = "Score", @operator = "GreaterThan", value = "50" }
            }
        });

        _evaluator.Evaluate(json, ctx).Should().BeTrue();
    }

    [Fact]
    public void AndCombination_OneFalse_ReturnsFalse()
    {
        var ctx = AutomationTestHelpers.CreateContext();
        ctx.SetVariable("Status", "Inactive");
        ctx.SetVariable("Score", "85");

        var json = JsonSerializer.Serialize(new
        {
            @operator = "And",
            conditions = new object[]
            {
                new { field = "Status", @operator = "Equals", value = "Active" },
                new { field = "Score", @operator = "GreaterThan", value = "50" }
            }
        });

        _evaluator.Evaluate(json, ctx).Should().BeFalse();
    }

    [Fact]
    public void OrCombination_OneFalse_ReturnsTrue()
    {
        var ctx = AutomationTestHelpers.CreateContext();
        ctx.SetVariable("Status", "Inactive");
        ctx.SetVariable("Score", "85");

        var json = JsonSerializer.Serialize(new
        {
            @operator = "Or",
            conditions = new object[]
            {
                new { field = "Status", @operator = "Equals", value = "Active" },
                new { field = "Score", @operator = "GreaterThan", value = "50" }
            }
        });

        _evaluator.Evaluate(json, ctx).Should().BeTrue();
    }

    [Fact]
    public void IsNull_NullValue_ReturnsTrue()
    {
        var ctx = AutomationTestHelpers.CreateContext();
        // "MissingField" is not set -> null

        var json = JsonSerializer.Serialize(new[]
        {
            new { field = "MissingField", @operator = "IsNull", value = (string?)null }
        });

        _evaluator.Evaluate(json, ctx).Should().BeTrue();
    }
}
