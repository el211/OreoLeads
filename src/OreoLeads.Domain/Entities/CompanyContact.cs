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

    // Navigation
    public Lead Lead { get; set; } = null!;
}
