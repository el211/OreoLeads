using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities.Automation;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Automation;

internal static class AutomationTestHelpers
{
    public static ApplicationDbContext CreateDbContext(TenantContext? tenant = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, tenant ?? new TenantContext());
    }

    public static AutomationWorkflow CreateWorkflow(
        string name = "Test Workflow",
        Guid? orgId = null,
        bool isEnabled = true,
        WorkflowStatus status = WorkflowStatus.Active)
    {
        return new AutomationWorkflow
        {
            Name = name,
            OrganizationId = orgId,
            IsEnabled = isEnabled,
            Status = status,
            ConcurrencyLimit = 1,
            TimeoutSeconds = 300
        };
    }

    public static AutomationAction CreateAction(
        Guid workflowId,
        ActionType type = ActionType.CreateNote,
        string name = "Test Action",
        int sortOrder = 0,
        bool continueOnError = false,
        string? configJson = null,
        Guid? orgId = null)
    {
        return new AutomationAction
        {
            WorkflowId = workflowId,
            Name = name,
            Type = type,
            SortOrder = sortOrder,
            ContinueOnError = continueOnError,
            ConfigJson = configJson,
            OrganizationId = orgId
        };
    }

    public static AutomationTrigger CreateTrigger(
        Guid workflowId,
        TriggerType type = TriggerType.Manual,
        Guid? orgId = null)
    {
        return new AutomationTrigger
        {
            WorkflowId = workflowId,
            Type = type,
            OrganizationId = orgId
        };
    }

    public static AutomationExecution CreateExecution(
        Guid workflowId,
        ExecutionStatus status = ExecutionStatus.Pending,
        Guid? orgId = null)
    {
        return new AutomationExecution
        {
            WorkflowId = workflowId,
            WorkflowName = "Test",
            TriggerType = TriggerType.Manual,
            Status = status,
            StartedAt = DateTime.UtcNow,
            OrganizationId = orgId
        };
    }

    public static AutomationSchedule CreateSchedule(
        Guid workflowId,
        ScheduleInterval interval = ScheduleInterval.Daily,
        DateTime? nextRunAt = null,
        bool isEnabled = true,
        Guid? orgId = null)
    {
        return new AutomationSchedule
        {
            WorkflowId = workflowId,
            Interval = interval,
            NextRunAt = nextRunAt ?? DateTime.UtcNow.AddMinutes(-1),
            IsEnabled = isEnabled,
            OrganizationId = orgId
        };
    }

    public static AutomationQueueItem CreateQueueItem(
        Guid workflowId,
        QueueItemStatus status = QueueItemStatus.Pending,
        int priority = 0,
        Guid? orgId = null)
    {
        return new AutomationQueueItem
        {
            WorkflowId = workflowId,
            Status = status,
            Priority = priority,
            OrganizationId = orgId
        };
    }

    public static AutomationTemplate CreateTemplate(
        string name = "Test Template",
        bool isBuiltIn = true)
    {
        return new AutomationTemplate
        {
            Name = name,
            Category = "Test",
            IsBuiltIn = isBuiltIn,
            TriggerJson = "{\"type\":\"LeadCreated\"}",
            ActionsJson = "[{\"type\":\"CreateNote\",\"config\":{\"content\":\"test\"}}]"
        };
    }

    public static AutomationContext CreateContext(
        Guid? workflowId = null,
        Guid? executionId = null,
        Guid? leadId = null,
        Guid? orgId = null)
    {
        return new AutomationContext
        {
            WorkflowId = workflowId ?? Guid.NewGuid(),
            ExecutionId = executionId ?? Guid.NewGuid(),
            LeadId = leadId,
            OrganizationId = orgId
        };
    }
}
