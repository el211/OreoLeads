using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

public class LeadNote : BaseEntity
{
    public Guid LeadId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Lead Lead { get; set; } = null!;
}
