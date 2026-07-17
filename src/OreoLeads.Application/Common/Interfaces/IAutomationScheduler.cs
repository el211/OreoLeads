using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Application.Common.Interfaces;

public interface IAutomationScheduler
{
    Task<DateTime?> GetNextRunTimeAsync(Guid scheduleId, CancellationToken ct = default);
    Task<List<AutomationSchedule>> GetDueSchedulesAsync(CancellationToken ct = default);
    Task UpdateNextRunAsync(Guid scheduleId, CancellationToken ct = default);
    Task PauseAsync(Guid scheduleId, CancellationToken ct = default);
    Task ResumeAsync(Guid scheduleId, CancellationToken ct = default);
}
