using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities.Automation;

public class AutomationExecutionError : BaseEntity
{
    public Guid ExecutionId { get; set; }
    public Guid? ActionId { get; set; }
    public string? ActionName { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public bool IsRetryable { get; set; }
    public Guid? OrganizationId { get; set; }

    // Navigation
    public AutomationExecution? Execution { get; set; }
}
