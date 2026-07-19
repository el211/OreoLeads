using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Analysis;
using OreoLeads.Infrastructure.Persistence;
using System.Text;
using System.Text.Json;

namespace OreoLeads.Infrastructure.Ai;

internal sealed class PromptBuilder : IPromptBuilder
{
    private readonly ApplicationDbContext _db;
    private readonly IPromptTemplateRepository _templates;

    public PromptBuilder(ApplicationDbContext db, IPromptTemplateRepository templates)
    {
        _db = db;
        _templates = templates;
    }

    public async Task<string> BuildEmailSystemPromptAsync(
        Guid leadId, GenerateEmailRequestDto request, CancellationToken ct = default)
    {
        var systemTemplate = await _templates.GetByKeyAsync("email.system");
        return systemTemplate?.Content ?? GetFallbackSystemPrompt();
    }

    public async Task<string> BuildEmailUserPromptAsync(
        Guid leadId, GenerateEmailRequestDto request, CancellationToken ct = default)
    {
        var lead = await _db.Leads
            .Include(l => l.WebsiteAnalyses)
            .FirstOrDefaultAsync(l => l.Id == leadId, ct)
            ?? throw new InvalidOperationException($"Lead {leadId} not found.");

        var analysis = lead.WebsiteAnalyses.OrderByDescending(a => a.CreatedAt).FirstOrDefault();

        // Load type-specific template
        var typeKey = request.Type switch
        {
            EmailType.FirstContact  => "email.first_contact",
            EmailType.FollowUp      => "email.follow_up",
            EmailType.Proposal      => "email.proposal",
            EmailType.AfterMeeting  => "email.after_meeting",
            EmailType.LastFollowUp  => "email.last_follow_up",
            _                       => "email.first_contact"
        };

        var typeTemplate = await _templates.GetByKeyAsync(typeKey);

        var sb = new StringBuilder();

        // ── Lead context ───────────────────────────────────────────────────────
        sb.AppendLine("=== INFORMATIONS PROSPECT ===");
        sb.AppendLine($"Entreprise : {lead.CompanyName}");
        if (!string.IsNullOrWhiteSpace(lead.TradeName))
            sb.AppendLine($"Nom commercial : {lead.TradeName}");
        if (!string.IsNullOrWhiteSpace(lead.Industry))
            sb.AppendLine($"Secteur : {lead.Industry}");
        if (!string.IsNullOrWhiteSpace(lead.City))
            sb.AppendLine($"Ville : {lead.City}" + (lead.Department != null ? $" ({lead.Department})" : ""));
        if (!string.IsNullOrWhiteSpace(lead.Website))
            sb.AppendLine($"Site web : {lead.Website}");
        if (lead.EmployeeCount.HasValue)
            sb.AppendLine($"Effectif : ~{lead.EmployeeCount} employés");
        if (!string.IsNullOrWhiteSpace(lead.Description))
            sb.AppendLine($"Description : {lead.Description}");

        // ── Website analysis ──────────────────────────────────────────────────
        if (analysis is not null)
        {
            sb.AppendLine();
            sb.AppendLine("=== ANALYSE DU SITE WEB ===");
            sb.AppendLine($"Score d'opportunité : {analysis.BusinessScore}/100");
            sb.AppendLine($"Statut HTTP : {analysis.HttpStatus} — {analysis.ResponseTimeMs} ms");
            sb.AppendLine($"HTTPS : {(analysis.UsesHttps ? "Oui" : "Non")}");
            if (!string.IsNullOrWhiteSpace(analysis.CmsDetected))
                sb.AppendLine($"CMS : {analysis.CmsDetected}");

            // Opportunities
            if (!string.IsNullOrWhiteSpace(analysis.Recommendations))
            {
                try
                {
                    var opps = JsonSerializer.Deserialize<List<string>>(analysis.Recommendations);
                    if (opps?.Count > 0)
                    {
                        sb.AppendLine("Opportunités identifiées :");
                        foreach (var o in opps)
                            sb.AppendLine($"  - {o}");
                    }
                }
                catch { /* ignore */ }
            }

            // Technologies
            if (!string.IsNullOrWhiteSpace(analysis.TechnologiesDetected))
            {
                try
                {
                    var techs = JsonSerializer.Deserialize<List<string>>(analysis.TechnologiesDetected);
                    if (techs?.Count > 0)
                        sb.AppendLine($"Technologies : {string.Join(", ", techs)}");
                }
                catch { /* ignore */ }
            }

            // Oreo services
            var oreoServices = BusinessRecommendationService.GetOreoServices(analysis, lead.Industry);
            if (oreoServices.Count > 0)
            {
                sb.AppendLine("Services Oreo Studios recommandés :");
                foreach (var s in oreoServices)
                    sb.AppendLine($"  - {s}");
            }

            if (!string.IsNullOrWhiteSpace(analysis.Summary))
                sb.AppendLine($"Résumé de l'analyse : {analysis.Summary}");
        }

        // ── Generation parameters ─────────────────────────────────────────────
        sb.AppendLine();
        sb.AppendLine("=== PARAMÈTRES DE GÉNÉRATION ===");
        sb.AppendLine($"Type d'email : {TypeLabel(request.Type)}");
        sb.AppendLine($"Style : {StyleLabel(request.Style)}");
        sb.AppendLine($"Longueur : {LengthLabel(request.Length)}");

        if (!string.IsNullOrWhiteSpace(request.CustomInstructions))
        {
            sb.AppendLine();
            sb.AppendLine("=== INSTRUCTIONS PERSONNALISÉES ===");
            sb.AppendLine(request.CustomInstructions);
        }

        // ── Type-specific instructions ────────────────────────────────────────
        if (typeTemplate is not null)
        {
            sb.AppendLine();
            sb.AppendLine("=== INSTRUCTIONS SPÉCIFIQUES ===");
            sb.AppendLine(typeTemplate.Content);
        }

        return sb.ToString();
    }

    private static string TypeLabel(EmailType t) => t switch
    {
        EmailType.FirstContact  => "Premier contact",
        EmailType.FollowUp      => "Relance",
        EmailType.Reply         => "Réponse",
        EmailType.Proposal      => "Proposition commerciale",
        EmailType.AfterMeeting  => "Suivi après rendez-vous",
        EmailType.LastFollowUp  => "Dernière relance",
        _                       => t.ToString()
    };

    private static string StyleLabel(EmailStyle s) => s switch
    {
        EmailStyle.Professional => "Professionnel",
        EmailStyle.Friendly     => "Chaleureux / Amical",
        EmailStyle.Premium      => "Haut de gamme / Premium",
        EmailStyle.Short        => "Court et direct",
        EmailStyle.Luxury       => "Luxe / Exclusif",
        EmailStyle.French       => "Français classique",
        EmailStyle.English      => "English (international)",
        _                       => s.ToString()
    };

    private static string LengthLabel(EmailLength l) => l switch
    {
        EmailLength.Short  => "Court (5-8 lignes max)",
        EmailLength.Normal => "Normal (10-15 lignes)",
        EmailLength.Long   => "Long (détaillé, 20+ lignes)",
        _                  => l.ToString()
    };

    private static string GetFallbackSystemPrompt() => """
Tu es un assistant commercial pour Oreo Studios, agence française spécialisée en :
- Développement web et applications sur mesure
- CRM de prospection et gestion client
- Automatisation des processus métier
- Formulaires de contact et systèmes de réservation optimisés
- Logiciels métier sur mesure

Règles impératives :
1. Commence TOUJOURS l'email par une présentation d'Oreo Studios et de ton interlocuteur (prénom si disponible).
2. Utilise les données d'analyse du site pour personnaliser le message : mentionne ce qui manque (formulaire, chat, etc.) et comment Oreo Studios peut y remédier.
3. Propose uniquement les services pertinents par rapport aux opportunités détectées.
4. Signe au nom d'Oreo Studios avec une formule professionnelle.
5. Ne mentionne jamais de SIREN/SIRET dans l'email — ce sont des informations internes.
6. Réponds UNIQUEMENT en JSON valide avec exactement ces clés : subject, body, summary, callToAction.
""";
}
