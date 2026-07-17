using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

/// <summary>Webhook event received from Brevo for an email send operation.</summary>
public class EmailEvent : BaseEntity
{
    public Guid?          EmailSendJobId { get; set; }
    public Guid?          LeadId         { get; set; }
    public EmailEventType EventType      { get; set; }
    public DateTime       OccurredAt     { get; set; }
    public string?        Email          { get; set; }
    public string?        MessageId      { get; set; }
    /// <summary>Additional event details serialized as JSON.</summary>
    public string?        Details        { get; set; }
    public string?        IpAddress      { get; set; }
}
