using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities.Automation;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Automation;

internal sealed class AutomationSchedulerService : IAutomationScheduler
{
    private readonly ApplicationDbContext _db;

    public AutomationSchedulerService(ApplicationDbContext db) => _db = db;

    public async Task<DateTime?> GetNextRunTimeAsync(Guid scheduleId, CancellationToken ct = default)
    {
        var schedule = await _db.AutomationSchedules.FindAsync([scheduleId], ct);
        return schedule?.NextRunAt;
    }

    public async Task<List<AutomationSchedule>> GetDueSchedulesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.AutomationSchedules
            .Where(s => s.IsEnabled && s.NextRunAt != null && s.NextRunAt <= now)
            .Where(s => s.ExpiresAt == null || s.ExpiresAt > now)
            .Where(s => s.MaxRuns == null || s.RunCount < s.MaxRuns)
            .Include(s => s.Workflow)
            .ToListAsync(ct);
    }

    public async Task UpdateNextRunAsync(Guid scheduleId, CancellationToken ct = default)
    {
        var schedule = await _db.AutomationSchedules.FindAsync([scheduleId], ct);
        if (schedule is null) return;

        schedule.LastRunAt = DateTime.UtcNow;
        schedule.RunCount++;

        var now = DateTime.UtcNow;
        schedule.NextRunAt = schedule.Interval switch
        {
            ScheduleInterval.EveryMinute => now.AddMinutes(1),
            ScheduleInterval.EveryHour => now.AddHours(1),
            ScheduleInterval.Daily => now.AddDays(1),
            ScheduleInterval.Weekly => now.AddDays(7),
            ScheduleInterval.Monthly => now.AddMonths(1),
            ScheduleInterval.Cron => CronHelper.GetNextOccurrence(schedule.CronExpression ?? "", now),
            _ => now.AddHours(1)
        };

        // Disable if max runs reached
        if (schedule.MaxRuns.HasValue && schedule.RunCount >= schedule.MaxRuns.Value)
            schedule.IsEnabled = false;

        // Disable if expired
        if (schedule.ExpiresAt.HasValue && schedule.NextRunAt > schedule.ExpiresAt)
            schedule.IsEnabled = false;

        schedule.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task PauseAsync(Guid scheduleId, CancellationToken ct = default)
    {
        var schedule = await _db.AutomationSchedules.FindAsync([scheduleId], ct);
        if (schedule is null) return;
        schedule.IsEnabled = false;
        schedule.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }

    public async Task ResumeAsync(Guid scheduleId, CancellationToken ct = default)
    {
        var schedule = await _db.AutomationSchedules.FindAsync([scheduleId], ct);
        if (schedule is null) return;
        schedule.IsEnabled = true;
        if (schedule.NextRunAt is null || schedule.NextRunAt < DateTime.UtcNow)
            schedule.NextRunAt = DateTime.UtcNow;
        schedule.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);
    }
}
