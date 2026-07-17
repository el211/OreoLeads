using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

/// <summary>Queue entry representing a pending or completed email send operation.</summary>
public class EmailSendJob : BaseEntity
{
    public Guid             GeneratedEmailId { get; set; }
    public Guid             LeadId           { get; set; }
    public EmailSendStatus  Status           { get; set; } = EmailSendStatus.Pending;
    public DateTime         ScheduledAt      { get; set; }
    public DateTime?        SentAt           { get; set; }
    public int              AttemptCount     { get; set; } = 0;
    public int              MaxAttempts      { get; set; } = 3;
    public DateTime?        NextAttemptAt    { get; set; }
    public DateTime?        LastAttemptAt    { get; set; }
    public string?          ErrorMessage     { get; set; }
    public string?          BrevoMessageId   { get; set; }
    public string           ToEmail          { get; set; } = string.Empty;
    public string?          ToName           { get; set; }
    public string           Subject          { get; set; } = string.Empty;
    public string           HtmlBody         { get; set; } = string.Empty;
    public Guid?            OrganizationId   { get; set; }
}
