using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

public class WebsiteAnalysis : BaseEntity
{
    public Guid LeadId { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public bool HasSsl { get; set; }
    public bool HasContact { get; set; }
    public bool HasBlog { get; set; }
    public bool HasSocialLinks { get; set; }
    public int? LoadTimeMs { get; set; }
    public string? TechStack { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public int Score { get; set; } = 0;
    public string? Notes { get; set; }

    // Navigation
    public Lead Lead { get; set; } = null!;
}
