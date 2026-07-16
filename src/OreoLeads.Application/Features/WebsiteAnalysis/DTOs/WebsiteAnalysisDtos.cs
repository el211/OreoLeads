namespace OreoLeads.Application.Features.WebsiteAnalysis.DTOs;

public class WebsiteAnalysisDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime LastAnalysis { get; set; }
    public DateTime CreatedAt { get; set; }

    // HTTP
    public int HttpStatus { get; set; }
    public int ResponseTimeMs { get; set; }
    public bool UsesHttps { get; set; }
    public bool CertificateValid { get; set; }
    public int RedirectCount { get; set; }

    // SEO
    public string? PageTitle { get; set; }
    public string? MetaDescription { get; set; }
    public bool HasViewport { get; set; }

    // Fonctionnalités
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
    public List<string> Technologies { get; set; } = new();

    // Scoring
    public int BusinessScore { get; set; }
    public string? Summary { get; set; }
    public List<string> Opportunities { get; set; } = new();
    public List<string> OreoServicesRecommended { get; set; } = new();
    public string? AnalysisError { get; set; }
}
