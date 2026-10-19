using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

public class OutreachReview : BaseEntity
{
    public Guid EmailDraftId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RecipientEmail { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string Basis { get; set; } = "";
    public string BasisEvidence { get; set; } = "";
    public string SenderIdentification { get; set; } = "";
    public string PrivacyNoticeUrl { get; set; } = "";
    public string OptOutUrl { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public string ReviewedByUserId { get; set; } = "";
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; }
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
