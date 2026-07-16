using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

public class WebsiteAnalysis : BaseEntity
{
    public Guid LeadId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime LastAnalysis { get; set; } = DateTime.UtcNow;

    // HTTP
    public int HttpStatus { get; set; }
    public int ResponseTimeMs { get; set; }
    public bool UsesHttps { get; set; }
    public bool CertificateValid { get; set; }
    public int RedirectCount { get; set; }

    // SEO / méta
    public string? PageTitle { get; set; }
    public string? MetaDescription { get; set; }
    public bool HasViewport { get; set; }

    // Fonctionnalités détectées
    public bool HasContactForm { get; set; }
    public bool HasQuoteForm { get; set; }
    public bool HasBookingSystem { get; set; }
    public bool HasChatWidget { get; set; }

    // Informations visibles
    public bool HasEmailVisible { get; set; }
    public bool HasPhoneVisible { get; set; }
    public bool HasAddressVisible { get; set; }

    // Conformité
    public bool HasPrivacyPolicy { get; set; }
    public bool HasLegalNotice { get; set; }

    // Technologie
    public string? CmsDetected { get; set; }
    public string? TechnologiesDetected { get; set; }  // JSON array

    // Scoring
    public int BusinessScore { get; set; }
    public string? Summary { get; set; }
    public string? Recommendations { get; set; }  // JSON array d'opportunités
    public string? AnalysisError { get; set; }

    
    // Navigation
    public Lead Lead { get; set; } = null!;
}
