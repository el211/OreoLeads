using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Automation;

namespace OreoLeads.Tests.Automation;

public class AutomationEngineTests
{
    private (AutomationEngine engine, Infrastructure.Persistence.ApplicationDbContext db) BuildEngine()
    {
        var db = AutomationTestHelpers.CreateDbContext();
        var executor = new AutomationExecutorService(
            new StubServiceProvider(),
            NullLogger<AutomationExecutorService>.Instance);
        var validator = new AutomationValidatorService();
        var engine = new AutomationEngine(db, executor, validator, NullLogger<AutomationEngine>.Instance);
        return (engine, db);
    }

    [Fact]
    public async Task TriggerAsync_NoMatchingWorkflow_ReturnsEmpty()
    {
        var (engine, _) = BuildEngine();
        var trigger = new TriggerEventDto(TriggerType.LeadCreated, null, null, new());

        var result = await engine.TriggerAsync(trigger);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("No matching");
    }

    [Fact]
    public async Task TriggerAsync_DisabledWorkflow_Skipped()
    {
        var (engine, db) = BuildEngine();
        var wf = AutomationTestHelpers.CreateWorkflow(isEnabled: false);
        db.AutomationWorkflows.Add(wf);
        db.AutomationTriggers.Add(AutomationTestHelpers.CreateTrigger(wf.Id, TriggerType.LeadCreated));
        await db.SaveChangesAsync();

        var trigger = new TriggerEventDto(TriggerType.LeadCreated, null, null, new());
        var result = await engine.TriggerAsync(trigger);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("No matching");
    }

    [Fact]
    public async Task ExecuteWorkflow_NoActions_ReturnsSuccess()
    {
        var (engine, db) = BuildEngine();
        var wf = AutomationTestHelpers.CreateWorkflow();
        db.AutomationWorkflows.Add(wf);
        await db.SaveChangesAsync();

        var trigger = new TriggerEventDto(TriggerType.Manual, null, null, new());
        var result = await engine.ExecuteWorkflowAsync(wf.Id, trigger);

        result.Success.Should().BeTrue();
        result.ExecutionId.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteWorkflow_ActionFails_ContinueOnError_Continues()
    {
        var (engine, db) = BuildEngine();
        var wf = AutomationTestHelpers.CreateWorkflow();
        db.AutomationWorkflows.Add(wf);

        // Action that will fail (unsupported type resolved via StubServiceProvider)
        // We use a type the StubServiceProvider won't resolve => throws
        var action = AutomationTestHelpers.CreateAction(wf.Id, ActionType.CreateNote, continueOnError: true, sortOrder: 0);
        db.AutomationActions.Add(action);
        await db.SaveChangesAsync();

        var trigger = new TriggerEventDto(TriggerType.Manual, null, null, new());
        var result = await engine.ExecuteWorkflowAsync(wf.Id, trigger);

        // Should complete (continueOnError=true) even though action fails
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteWorkflow_ActionFails_StopOnError_Stops()
    {
        var (engine, db) = BuildEngine();
        var wf = AutomationTestHelpers.CreateWorkflow();
        db.AutomationWorkflows.Add(wf);

        var action = AutomationTestHelpers.CreateAction(wf.Id, ActionType.CreateNote, continueOnError: false, sortOrder: 0);
        db.AutomationActions.Add(action);
        await db.SaveChangesAsync();

        var trigger = new TriggerEventDto(TriggerType.Manual, null, null, new());
        var result = await engine.ExecuteWorkflowAsync(wf.Id, trigger);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteWorkflow_MaxExecutionsReached_Skipped()
    {
        var (engine, db) = BuildEngine();
        var wf = AutomationTestHelpers.CreateWorkflow();
        wf.MaxExecutions = 5;
        wf.ExecutionCount = 5;
        db.AutomationWorkflows.Add(wf);
        await db.SaveChangesAsync();

        var trigger = new TriggerEventDto(TriggerType.Manual, null, null, new());
        var result = await engine.ExecuteWorkflowAsync(wf.Id, trigger);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Max executions");
    }

    [Fact]
    public async Task CancelExecution_Running_SetsCancelled()
    {
        var (engine, db) = BuildEngine();
        var wf = AutomationTestHelpers.CreateWorkflow();
        db.AutomationWorkflows.Add(wf);

        var execution = AutomationTestHelpers.CreateExecution(wf.Id, ExecutionStatus.Running);
        db.AutomationExecutions.Add(execution);
        await db.SaveChangesAsync();

        await engine.CancelExecutionAsync(execution.Id);

        var updated = await db.AutomationExecutions.FindAsync(execution.Id);
        updated!.Status.Should().Be(ExecutionStatus.Cancelled);
    }

    [Fact]
    public async Task ExecuteWorkflow_InfiniteLoopProtection_Stops()
    {
        // Test that ExecuteWorkflow with no actions but max executions = 0 returns error
        var (engine, db) = BuildEngine();
        var wf = AutomationTestHelpers.CreateWorkflow();
        wf.MaxExecutions = 0;
        db.AutomationWorkflows.Add(wf);
        await db.SaveChangesAsync();

        var trigger = new TriggerEventDto(TriggerType.Manual, null, null, new());
        var result = await engine.ExecuteWorkflowAsync(wf.Id, trigger);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Max executions");
    }
}

/// <summary>
/// Stub service provider for engine tests - all action handler lookups will throw,
/// simulating action failure for testing error handling.
/// </summary>
internal class StubServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}
