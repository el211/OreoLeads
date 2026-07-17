using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities.Automation;

public class AutomationExecutionLog : BaseEntity
{
    public Guid ExecutionId { get; set; }
    public Guid? ActionId { get; set; }
    public string? ActionName { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = "Info";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Data { get; set; }
    public Guid? OrganizationId { get; set; }

    // Navigation
    public AutomationExecution? Execution { get; set; }
}
