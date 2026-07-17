using System.Text.Json;
using FluentAssertions;
using OreoLeads.Domain.Entities.Automation;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Automation;

namespace OreoLeads.Tests.Automation;

public class AutomationValidatorTests
{
    private readonly AutomationValidatorService _validator = new();

    [Fact]
    public async Task ValidateWorkflow_EmptyName_ReturnsError()
    {
        var workflow = new AutomationWorkflow
        {
            Name = "",
            TimeoutSeconds = 300,
            ConcurrencyLimit = 1
        };

        var result = await _validator.ValidateWorkflowAsync(workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("name"));
    }

    [Fact]
    public async Task ValidateWorkflow_ValidWorkflow_ReturnsValid()
    {
        var workflow = new AutomationWorkflow
        {
            Name = "Test Workflow",
            TimeoutSeconds = 300,
            ConcurrencyLimit = 1
        };

        var result = await _validator.ValidateWorkflowAsync(workflow);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void HasCircularReference_DirectSelf_ReturnsTrue()
    {
        var workflowId = Guid.NewGuid();
        var actionsJson = JsonSerializer.Serialize(new[]
        {
            new { type = "ExecuteWorkflow", config = new { workflowId = workflowId.ToString() } }
        });

        _validator.HasCircularReference(workflowId, actionsJson).Should().BeTrue();
    }

    [Fact]
    public void ExceedsDepthLimit_DeepNesting_ReturnsTrue()
    {
        // Create deeply nested JSON
        var json = "[[[[[[[[[[[[[]]]]]]]]]]]]]"; // 13 levels

        _validator.ExceedsDepthLimit(json, 10).Should().BeTrue();
    }

    [Fact]
    public void ValidateConditions_MalformedJson_ReturnsFalse()
    {
        var ctx = AutomationTestHelpers.CreateContext();

        var result = _validator.EvaluateConditions("not valid json {{{", ctx);

        result.Should().BeFalse();
    }
}
