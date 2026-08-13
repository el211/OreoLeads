using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

/// <summary>Queue entry representing a pending or completed SMS send operation.</summary>
public class SmsSendJob : BaseEntity
{
    public Guid          LeadId        { get; set; }
    public SmsSendStatus Status        { get; set; } = SmsSendStatus.Pending;
    public DateTime      ScheduledAt   { get; set; }
    public DateTime?     SentAt        { get; set; }
    public int           AttemptCount  { get; set; } = 0;
    public int           MaxAttempts   { get; set; } = 3;
    public DateTime?     NextAttemptAt { get; set; }
    public DateTime?     LastAttemptAt { get; set; }
    public string?       ErrorMessage  { get; set; }
    public string?       BrevoMessageId { get; set; }
    public string        ToPhone       { get; set; } = string.Empty;
    public string?       ToName        { get; set; }
    public string        Message       { get; set; } = string.Empty;
    public Guid?         OrganizationId { get; set; }
}
