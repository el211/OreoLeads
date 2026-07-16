using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }   // JSON
    public string? NewValues { get; set; }   // JSON
    public string? IpAddress { get; set; }
    public bool Succeeded { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
