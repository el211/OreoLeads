using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

public class Lead : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Department { get; set; }
    public string? Region { get; set; }
    public string Country { get; set; } = "France";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Siren { get; set; }
    public string? Siret { get; set; }
    public string? NafCode { get; set; }
    public int? EmployeeCount { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public LeadPriority Priority { get; set; } = LeadPriority.Medium;
    public int Score { get; set; } = 0;
    public Guid? AssignedUserId { get; set; }

    // Navigation
    public ICollection<LeadActivity> Activities { get; set; } = new List<LeadActivity>();
    public ICollection<LeadNote> Notes { get; set; } = new List<LeadNote>();
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public ICollection<LeadTag> LeadTags { get; set; } = new List<LeadTag>();
    public ICollection<WebsiteAnalysis> WebsiteAnalyses { get; set; } = new List<WebsiteAnalysis>();
    public ICollection<GeneratedEmail> GeneratedEmails { get; set; } = new List<GeneratedEmail>();
    public ICollection<CompanyContact> CompanyContacts { get; set; } = new List<CompanyContact>();
}
