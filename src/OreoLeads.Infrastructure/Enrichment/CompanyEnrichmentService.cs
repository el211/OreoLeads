using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Enrichment;

public interface ICompanyEnrichmentService
{
    /// <summary>Exécute l'enrichissement complet d'un job. Retourne le statut final.</summary>
    Task<EnrichmentStatus> RunAsync(Guid enrichmentId, CancellationToken ct = default);
}

/// <summary>
/// Orchestrateur d'enrichissement : découverte du site (Brave), découverte de
/// l'e-mail, analyse du site, traçabilité. Règles :
/// - score ≥ AutoApplyThreshold → application automatique au Lead ;
/// - score intermédiaire → NeedsReview (candidats conservés) ;
/// - une donnée validée manuellement n'est JAMAIS écrasée ;
/// - un e-mail n'est jamais inventé.
/// </summary>
public sealed class CompanyEnrichmentService : ICompanyEnrichmentService
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _db;
    private readonly IWebsiteDiscoveryService _websiteDiscovery;
    private readonly IEmailDiscoveryService _emailDiscovery;
    private readonly IWebsiteAnalyzerService _analyzer;
    private readonly EnrichmentSettings _settings;
    private readonly ILogger<CompanyEnrichmentService> _logger;

    public CompanyEnrichmentService(
        ApplicationDbContext db,
        IWebsiteDiscoveryService websiteDiscovery,
        IEmailDiscoveryService emailDiscovery,
        IWebsiteAnalyzerService analyzer,
        IOptions<EnrichmentSettings> settings,
        ILogger<CompanyEnrichmentService> logger)
    {
        _db               = db;
        _websiteDiscovery = websiteDiscovery;
        _emailDiscovery   = emailDiscovery;
        _analyzer         = analyzer;
        _settings         = settings.Value;
        _logger           = logger;
    }

    public async Task<EnrichmentStatus> RunAsync(Guid enrichmentId, CancellationToken ct = default)
    {
        var job = await _db.LeadEnrichments.FindAsync([enrichmentId], ct)
            ?? throw new InvalidOperationException($"LeadEnrichment {enrichmentId} not found.");
        var lead = await _db.Leads.FindAsync([job.LeadId], ct)
            ?? throw new InvalidOperationException($"Lead {job.LeadId} not found.");

        var needsReview = false;
        var externalProfiles = new List<ExternalProfile>();

        // ── 1. Découverte du site web ─────────────────────────────────────────
        var websiteToUse = lead.Website;

        if (string.IsNullOrWhiteSpace(lead.Website) && _websiteDiscovery.IsConfigured)
        {
            var discovery = await _websiteDiscovery.DiscoverAsync(lead, ct);

            job.WebsiteCandidatesJson = JsonSerializer.Serialize(discovery.Candidates, JsonOpts);
            job.SocialProfilesJson    = JsonSerializer.Serialize(discovery.ExternalProfiles, JsonOpts);
            job.MatchedSignalsJson    = JsonSerializer.Serialize(discovery.MatchedSignals, JsonOpts);
            job.WebsiteConfidence     = discovery.Confidence;
            job.SearchQueriesUsed    += discovery.QueriesUsed;
            externalProfiles          = discovery.ExternalProfiles;

            if (discovery.ChosenUrl is not null && discovery.Confidence >= _settings.AutoApplyThreshold)
            {
                job.ChosenWebsiteUrl = discovery.ChosenUrl;
                if (lead.WebsiteValidatedAt is null)
                {
                    lead.Website   = discovery.ChosenUrl;
                    websiteToUse   = discovery.ChosenUrl;
                    job.AutoApplied = true;
                    lead.SetUpdatedAt();
                }
            }
            else if (discovery.ChosenUrl is not null && discovery.Confidence >= _settings.ReviewThreshold)
            {
                // Candidat plausible mais pas assez sûr : revue manuelle
                job.ChosenWebsiteUrl = discovery.ChosenUrl;
                needsReview = true;
            }

            _logger.LogInformation(
                "Enrichissement {JobId} : site {Url} (confiance {Confidence:P0}, {Queries} requêtes Brave)",
                job.Id, discovery.ChosenUrl ?? "non trouvé", discovery.Confidence, discovery.QueriesUsed);
        }

        // ── 2. Découverte de l'e-mail ─────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(lead.Email))
        {
            EmailDiscoveryResult emailResult;
            if (!string.IsNullOrWhiteSpace(websiteToUse))
                emailResult = await _emailDiscovery.DiscoverFromWebsiteAsync(websiteToUse, ct);
            else
                emailResult = await _emailDiscovery.DiscoverFromExternalProfilesAsync(externalProfiles, ct);

            if (emailResult.Email is not null)
            {
                job.DiscoveredEmail = emailResult.Email;
                job.EmailSourceUrl  = emailResult.SourceUrl;
                job.EmailSourceType = emailResult.SourceType;
                job.EmailKind       = emailResult.Kind;
                job.EmailConfidence = emailResult.Confidence;

                if (emailResult.Confidence >= _settings.AutoApplyThreshold && lead.EmailValidatedAt is null)
                {
                    lead.Email = emailResult.Email;
                    lead.SetUpdatedAt();
                }
                else
                {
                    needsReview = true;
                }
            }
        }

        // ── 3. Analyse du site appliqué ───────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(lead.Website))
        {
            try
            {
                var analysis = await _analyzer.AnalyzeAsync(lead.Id, lead.Website!, ct);
                analysis.OrganizationId = lead.OrganizationId;
                _db.WebsiteAnalyses.Add(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Analyse du site {Url} échouée pendant l'enrichissement", lead.Website);
            }
        }

        // ── 4. Traçabilité ────────────────────────────────────────────────────
        _db.LeadActivities.Add(new LeadActivity
        {
            LeadId = lead.Id,
            Type = ActivityType.Enriched,
            Description = BuildActivityDescription(job),
        });

        job.SetUpdatedAt();
        await _db.SaveChangesAsync(ct);

        return needsReview ? EnrichmentStatus.NeedsReview : EnrichmentStatus.Completed;
    }

    private static string BuildActivityDescription(LeadEnrichment job)
    {
        var parts = new List<string>();
        if (job.ChosenWebsiteUrl is not null)
            parts.Add($"site {job.ChosenWebsiteUrl} (confiance {job.WebsiteConfidence:P0}{(job.AutoApplied ? ", appliqué" : ", à vérifier")})");
        if (job.DiscoveredEmail is not null)
            parts.Add($"e-mail {job.DiscoveredEmail} via {job.EmailSourceType}");
        return parts.Count > 0
            ? $"Enrichissement : {string.Join(" ; ", parts)}"
            : "Enrichissement : aucun site ni e-mail public trouvé";
    }
}
