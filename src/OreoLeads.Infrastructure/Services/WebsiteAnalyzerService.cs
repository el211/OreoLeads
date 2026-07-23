using System.Text.Json;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.WebsiteAnalysis.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Analysis;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Infrastructure.Services;

public class WebsiteAnalyzerService : IWebsiteAnalyzerService
{
    private readonly IPageFetcher _fetcher;
    private readonly ILogger<WebsiteAnalyzerService> _logger;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public WebsiteAnalyzerService(
        IPageFetcher fetcher,
        ILogger<WebsiteAnalyzerService> logger)
    {
        _fetcher = fetcher;
        _logger = logger;
    }

    public async Task<WebsiteAnalysis> AnalyzeAsync(
        Guid leadId, string url, CancellationToken ct = default)
    {
        url = NormalizeUrl(url);

        // SSRF protection — validate URL before any outbound request
        await SsrfGuard.ValidateAsync(url, ct);

        var analysis = new WebsiteAnalysis
        {
            LeadId = leadId,
            Url = url,
            LastAnalysis = DateTime.UtcNow,
        };

        var page = await _fetcher.FetchAsync(url, ct);
        var html = page.Html;

        analysis.HttpStatus = page.StatusCode;
        analysis.ResponseTimeMs = page.ResponseTimeMs;
        analysis.UsesHttps = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        analysis.CertificateValid = page.CertificateValid;
        analysis.RedirectCount = page.RedirectCount;
        analysis.AnalysisError = page.Error;
        analysis.AnalyzedWithBrowser = page.UsedBrowser;

        if (!string.IsNullOrEmpty(html))
        {
            analysis.PageTitle = HtmlAnalyzer.ExtractTitle(html);
            analysis.MetaDescription = HtmlAnalyzer.ExtractMetaDescription(html);
            analysis.HasViewport = HtmlAnalyzer.HasViewport(html);
            analysis.HasContactForm = HtmlAnalyzer.HasContactForm(html);
            analysis.HasQuoteForm = HtmlAnalyzer.HasQuoteForm(html);
            analysis.HasBookingSystem = HtmlAnalyzer.HasBookingSystem(html);
            analysis.HasChatWidget = HtmlAnalyzer.HasChatWidget(html);
            analysis.HasNewsletterForm = HtmlAnalyzer.HasNewsletterForm(html);
            analysis.HasWhatsApp = HtmlAnalyzer.HasWhatsAppLink(html);
            analysis.HasMessenger = HtmlAnalyzer.HasMessengerLink(html);
            analysis.BookingProvider = HtmlAnalyzer.DetectBookingProvider(html);
            analysis.HasEmailVisible = HtmlAnalyzer.HasEmailVisible(html);
            analysis.HasPhoneVisible = HtmlAnalyzer.HasPhoneVisible(html);
            analysis.HasAddressVisible = HtmlAnalyzer.HasAddressVisible(html);
            analysis.HasPrivacyPolicy = HtmlAnalyzer.HasPrivacyPolicy(html);
            analysis.HasLegalNotice = HtmlAnalyzer.HasLegalNotice(html);

            var technologies = TechnologyDetector.Detect(html);
            analysis.TechnologiesDetected = JsonSerializer.Serialize(technologies);
            analysis.CmsDetected = TechnologyDetector.DetectCms(html);

            // La page d'accueil ne contient souvent pas le formulaire :
            // analyser aussi la page contact/devis/rendez-vous si elle existe
            if (!analysis.HasContactForm)
                await AnalyzeContactPagesAsync(analysis, html, url, ct);
        }

        return Recalculate(analysis);
    }

    public WebsiteAnalysis Recalculate(WebsiteAnalysis a)
    {
        a.BusinessScore = BusinessScoringService.Calculate(a);
        var opportunities = BusinessScoringService.GetOpportunities(a);
        a.Recommendations = JsonSerializer.Serialize(opportunities);
        a.Summary = BuildSummary(a, opportunities);
        return a;
    }

    public WebsiteAnalysisDto ToDto(WebsiteAnalysis a, string? industry = null)
    {
        List<string> technologies = new();
        if (!string.IsNullOrEmpty(a.TechnologiesDetected))
        {
            try { technologies = JsonSerializer.Deserialize<List<string>>(a.TechnologiesDetected, JsonOpts) ?? new(); }
            catch { /* ignore */ }
        }

        List<string> opportunities = new();
        if (!string.IsNullOrEmpty(a.Recommendations))
        {
            try { opportunities = JsonSerializer.Deserialize<List<string>>(a.Recommendations, JsonOpts) ?? new(); }
            catch { /* ignore */ }
        }

        var oreoServices = BusinessRecommendationService.GetOreoServices(a, industry);

        return new WebsiteAnalysisDto
        {
            Id = a.Id,
            LeadId = a.LeadId,
            Url = a.Url,
            LastAnalysis = a.LastAnalysis,
            CreatedAt = a.CreatedAt,
            HttpStatus = a.HttpStatus,
            ResponseTimeMs = a.ResponseTimeMs,
            UsesHttps = a.UsesHttps,
            CertificateValid = a.CertificateValid,
            RedirectCount = a.RedirectCount,
            PageTitle = a.PageTitle,
            MetaDescription = a.MetaDescription,
            HasViewport = a.HasViewport,
            HasContactForm = a.HasContactForm,
            HasQuoteForm = a.HasQuoteForm,
            HasBookingSystem = a.HasBookingSystem,
            HasChatWidget = a.HasChatWidget,
            HasEmailVisible = a.HasEmailVisible,
            HasPhoneVisible = a.HasPhoneVisible,
            HasAddressVisible = a.HasAddressVisible,
            HasPrivacyPolicy = a.HasPrivacyPolicy,
            HasLegalNotice = a.HasLegalNotice,
            CmsDetected = a.CmsDetected,
            Technologies = technologies,
            BusinessScore = a.BusinessScore,
            Summary = a.Summary,
            Opportunities = opportunities,
            OreoServicesRecommended = oreoServices,
            AnalysisError = a.AnalysisError,
        };
    }

    private async Task AnalyzeContactPagesAsync(
        WebsiteAnalysis analysis, string homeHtml, string baseUrl, CancellationToken ct)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)) return;

        foreach (var contactUrl in HtmlAnalyzer.FindContactPageUrls(homeHtml, baseUri).Take(2))
        {
            try
            {
                await SsrfGuard.ValidateAsync(contactUrl, ct);
                var contactPage = await _fetcher.FetchAsync(contactUrl, ct);
                var contactHtml = contactPage.Html;
                if (string.IsNullOrEmpty(contactHtml)) continue;

                analysis.HasContactForm     |= HtmlAnalyzer.HasContactForm(contactHtml);
                analysis.HasQuoteForm       |= HtmlAnalyzer.HasQuoteForm(contactHtml);
                analysis.HasBookingSystem   |= HtmlAnalyzer.HasBookingSystem(contactHtml);
                analysis.HasEmailVisible    |= HtmlAnalyzer.HasEmailVisible(contactHtml);
                analysis.HasPhoneVisible    |= HtmlAnalyzer.HasPhoneVisible(contactHtml);
                analysis.BookingProvider    ??= HtmlAnalyzer.DetectBookingProvider(contactHtml);

                if (analysis.HasContactForm) break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Analyse de la page contact {Url} ignorée", contactUrl);
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string NormalizeUrl(string url)
    {
        url = url.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
         && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;
        return url;
    }

    private static string BuildSummary(WebsiteAnalysis a, List<string> opportunities)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Score business : {a.BusinessScore}/100");
        sb.AppendLine();
        sb.AppendLine($"URL analysée : {a.Url}");
        sb.AppendLine($"HTTP {a.HttpStatus} · {a.ResponseTimeMs} ms · " +
                      (a.UsesHttps ? "HTTPS ✓" : "HTTP (non sécurisé)"));
        if (!string.IsNullOrEmpty(a.CmsDetected))
            sb.AppendLine($"CMS : {a.CmsDetected}");

        if (opportunities.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Opportunités détectées :");
            foreach (var o in opportunities)
                sb.AppendLine($"  • {o}");
        }
        return sb.ToString().TrimEnd();
    }
}
