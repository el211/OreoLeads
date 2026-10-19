using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

public class CompanyContact : BaseEntity
{
    public Guid LeadId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Position { get; set; }
    public bool IsMain { get; set; } = false;
    public string? SourceUrl { get; set; }
    public string? ProfileUrl { get; set; }
    public double Confidence { get; set; }
    public string VerificationStatus { get; set; } = "Unknown";
    public DateTime? ObservedAt { get; set; }

    // Navigation
    public Lead Lead { get; set; } = null!;
}
