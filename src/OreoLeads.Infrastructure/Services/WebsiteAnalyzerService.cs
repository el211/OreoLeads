using System.Diagnostics;
using System.Net;
using System.Net.Security;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebsiteAnalyzerService> _logger;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public WebsiteAnalyzerService(
        IHttpClientFactory httpClientFactory,
        ILogger<WebsiteAnalyzerService> logger)
    {
        _httpClientFactory = httpClientFactory;
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

        var sw = Stopwatch.StartNew();
        var (html, httpResult) = await FetchPageAsync(url, ct);
        sw.Stop();

        analysis.HttpStatus = httpResult.StatusCode;
        analysis.ResponseTimeMs = (int)sw.ElapsedMilliseconds;
        analysis.UsesHttps = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        analysis.CertificateValid = httpResult.CertificateValid;
        analysis.RedirectCount = httpResult.RedirectCount;
        analysis.AnalysisError = httpResult.Error;

        if (!string.IsNullOrEmpty(html))
        {
            analysis.PageTitle = HtmlAnalyzer.ExtractTitle(html);
            analysis.MetaDescription = HtmlAnalyzer.ExtractMetaDescription(html);
            analysis.HasViewport = HtmlAnalyzer.HasViewport(html);
            analysis.HasContactForm = HtmlAnalyzer.HasContactForm(html);
            analysis.HasQuoteForm = HtmlAnalyzer.HasQuoteForm(html);
            analysis.HasBookingSystem = HtmlAnalyzer.HasBookingSystem(html);
            analysis.HasChatWidget = HtmlAnalyzer.HasChatWidget(html);
            analysis.HasEmailVisible = HtmlAnalyzer.HasEmailVisible(html);
            analysis.HasPhoneVisible = HtmlAnalyzer.HasPhoneVisible(html);
            analysis.HasAddressVisible = HtmlAnalyzer.HasAddressVisible(html);
            analysis.HasPrivacyPolicy = HtmlAnalyzer.HasPrivacyPolicy(html);
            analysis.HasLegalNotice = HtmlAnalyzer.HasLegalNotice(html);

            var technologies = TechnologyDetector.Detect(html);
            analysis.TechnologiesDetected = JsonSerializer.Serialize(technologies);
            analysis.CmsDetected = TechnologyDetector.DetectCms(html);
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

    // ── Fetch ─────────────────────────────────────────────────────────────────

    private async Task<(string? Html, FetchResult Result)> FetchPageAsync(
        string url, CancellationToken ct)
    {
        // Tentative 1 : client avec validation SSL
        var (html, result) = await TryFetchAsync(url, validateSsl: true, ct);
        if (result.Error != null && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // SSL invalide → réessai sans validation pour quand même analyser le HTML
            _logger.LogWarning("SSL invalide pour {Url}, réessai sans validation", url);
            var (html2, result2) = await TryFetchAsync(url, validateSsl: false, ct);
            return (html2, result2 with { CertificateValid = false });
        }
        return (html, result);
    }

    private async Task<(string? Html, FetchResult Result)> TryFetchAsync(
        string url, bool validateSsl, CancellationToken ct)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            ServerCertificateCustomValidationCallback =
                validateSsl ? null : (_, _, _, _) => true,
        };

        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15),
        };
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (compatible; OreoLeads/1.0; +https://oreostudios.fr)");
        client.DefaultRequestHeaders.Add("Accept-Language", "fr-FR,fr;q=0.9");

        try
        {
            var response = await client.GetAsync(url,
                HttpCompletionOption.ResponseContentRead, ct);

            var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
            var redirected = !string.Equals(url, finalUrl, StringComparison.OrdinalIgnoreCase);

            var html = string.Empty;
            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                html = await response.Content.ReadAsStringAsync(ct);
            }

            return (html, new FetchResult(
                StatusCode: (int)response.StatusCode,
                CertificateValid: url.StartsWith("https://") && validateSsl,
                RedirectCount: redirected ? 1 : 0,
                Error: null));
        }
        catch (HttpRequestException ex) when (
            ex.InnerException is System.Security.Authentication.AuthenticationException)
        {
            return (null, new FetchResult(0, false, 0,
                Error: $"Certificat SSL invalide : {ex.InnerException.Message}"));
        }
        catch (TaskCanceledException)
        {
            return (null, new FetchResult(0, false, 0, Error: "Timeout (>15 s)"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erreur de fetch pour {Url}", url);
            return (null, new FetchResult(0, false, 0, Error: ex.Message));
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

    private record FetchResult(int StatusCode, bool CertificateValid, int RedirectCount, string? Error);
}
