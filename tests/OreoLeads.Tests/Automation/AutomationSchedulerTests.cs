using FluentAssertions;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Automation;

namespace OreoLeads.Tests.Automation;

public class AutomationSchedulerTests
{
    [Fact]
    public async Task GetDueSchedules_PastNextRunAt_ReturnsSchedule()
    {
        var db = AutomationTestHelpers.CreateDbContext();
        var workflow = AutomationTestHelpers.CreateWorkflow();
        db.AutomationWorkflows.Add(workflow);

        var schedule = AutomationTestHelpers.CreateSchedule(workflow.Id, nextRunAt: DateTime.UtcNow.AddMinutes(-5));
        db.AutomationSchedules.Add(schedule);
        await db.SaveChangesAsync();

        var svc = new AutomationSchedulerService(db);
        var due = await svc.GetDueSchedulesAsync();

        due.Should().HaveCount(1);
        due[0].Id.Should().Be(schedule.Id);
    }

    [Fact]
    public async Task GetDueSchedules_FutureNextRunAt_ReturnsEmpty()
    {
        var db = AutomationTestHelpers.CreateDbContext();
        var workflow = AutomationTestHelpers.CreateWorkflow();
        db.AutomationWorkflows.Add(workflow);

        var schedule = AutomationTestHelpers.CreateSchedule(workflow.Id, nextRunAt: DateTime.UtcNow.AddHours(1));
        db.AutomationSchedules.Add(schedule);
        await db.SaveChangesAsync();

        var svc = new AutomationSchedulerService(db);
        var due = await svc.GetDueSchedulesAsync();

        due.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateNextRun_EveryMinute_IncrementsOneMinute()
    {
        var db = AutomationTestHelpers.CreateDbContext();
        var workflow = AutomationTestHelpers.CreateWorkflow();
        db.AutomationWorkflows.Add(workflow);

        var schedule = AutomationTestHelpers.CreateSchedule(workflow.Id, ScheduleInterval.EveryMinute);
        db.AutomationSchedules.Add(schedule);
        await db.SaveChangesAsync();

        var svc = new AutomationSchedulerService(db);
        var before = DateTime.UtcNow;
        await svc.UpdateNextRunAsync(schedule.Id);

        var updated = await db.AutomationSchedules.FindAsync(schedule.Id);
        updated!.NextRunAt.Should().BeAfter(before);
        updated.RunCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateNextRun_Daily_IncrementsOneDay()
    {
        var db = AutomationTestHelpers.CreateDbContext();
        var workflow = AutomationTestHelpers.CreateWorkflow();
        db.AutomationWorkflows.Add(workflow);

        var schedule = AutomationTestHelpers.CreateSchedule(workflow.Id, ScheduleInterval.Daily);
        db.AutomationSchedules.Add(schedule);
        await db.SaveChangesAsync();

        var svc = new AutomationSchedulerService(db);
        var before = DateTime.UtcNow;
        await svc.UpdateNextRunAsync(schedule.Id);

        var updated = await db.AutomationSchedules.FindAsync(schedule.Id);
        updated!.NextRunAt.Should().BeAfter(before.AddHours(23));
    }

    [Fact]
    public void UpdateNextRun_Cron_CalculatesCorrectNext()
    {
        // Test the CronHelper directly
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var next = CronHelper.GetNextOccurrence("0 9 * * *", from);

        next.Should().NotBeNull();
        next!.Value.Hour.Should().Be(9);
        next.Value.Minute.Should().Be(0);
    }

    [Fact]
    public async Task Pause_DisablesSchedule()
    {
        var db = AutomationTestHelpers.CreateDbContext();
        var workflow = AutomationTestHelpers.CreateWorkflow();
        db.AutomationWorkflows.Add(workflow);

        var schedule = AutomationTestHelpers.CreateSchedule(workflow.Id);
        db.AutomationSchedules.Add(schedule);
        await db.SaveChangesAsync();

        var svc = new AutomationSchedulerService(db);
        await svc.PauseAsync(schedule.Id);

        var updated = await db.AutomationSchedules.FindAsync(schedule.Id);
        updated!.IsEnabled.Should().BeFalse();
    }
}
