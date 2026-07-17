using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities.Automation;

public class AutomationSchedule : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public ScheduleInterval Interval { get; set; }
    public string? CronExpression { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string Timezone { get; set; } = "UTC";
    public DateTime? ExpiresAt { get; set; }
    public int? MaxRuns { get; set; }
    public int RunCount { get; set; }
    public Guid? OrganizationId { get; set; }

    // Navigation
    public AutomationWorkflow? Workflow { get; set; }
}
