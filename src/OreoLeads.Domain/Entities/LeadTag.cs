namespace OreoLeads.Domain.Entities;

public class LeadTag
{
    public Guid LeadId { get; set; }
    public Guid TagId { get; set; }

    // Navigation
    public Lead Lead { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
