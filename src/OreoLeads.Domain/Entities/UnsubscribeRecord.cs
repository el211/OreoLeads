using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

/// <summary>
/// Records an unsubscribe event for an email address.
/// Note: when created, the corresponding Lead's DoNotContact flag should be set to true.
/// </summary>
public class UnsubscribeRecord : BaseEntity
{
    public Guid?   LeadId         { get; set; }
    public string  Email          { get; set; } = string.Empty;
    public DateTime UnsubscribedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Origin of the unsubscribe: "webhook", "manual", etc.</summary>
    public string  Source         { get; set; } = "webhook";
    public string? Reason         { get; set; }
}
