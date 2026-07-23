using OreoLeads.Domain.Common;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Domain.Entities;

/// <summary>
/// Une exécution d'enrichissement pour un lead : découverte du site officiel
/// (Brave Search), découverte de l'e-mail public, traçabilité des sources et
/// des scores de confiance. Historisé : plusieurs lignes par lead possibles.
/// </summary>
public class LeadEnrichment : BaseEntity
{
    public Guid LeadId { get; set; }
    public Guid? OrganizationId { get; set; }
    public EnrichmentStatus Status { get; set; } = EnrichmentStatus.Pending;

    // ── Mécanique de file (même modèle que EmailSendJob) ─────────────────────
    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public DateTime? NextAttemptAt { get; set; }
    public string? ErrorMessage { get; set; }

    // ── Découverte du site web ────────────────────────────────────────────────
    /// <summary>JSON : [{url, score, category, signals[]}]</summary>
    public string? WebsiteCandidatesJson { get; set; }
    public string? ChosenWebsiteUrl { get; set; }
    public double? WebsiteConfidence { get; set; }
    /// <summary>JSON : ["name","city","phone","siren","domain",…]</summary>
    public string? MatchedSignalsJson { get; set; }
    /// <summary>JSON : sources secondaires catégorisées (réseaux sociaux, annuaires, réservation, registres).</summary>
    public string? SocialProfilesJson { get; set; }

    // ── Découverte de l'e-mail (jamais deviné) ───────────────────────────────
    public string? DiscoveredEmail { get; set; }
    public string? EmailSourceUrl { get; set; }
    /// <summary>Website, Facebook, PagesJaunes…</summary>
    public string? EmailSourceType { get; set; }
    public DiscoveredEmailKind EmailKind { get; set; } = DiscoveredEmailKind.Unknown;
    public double? EmailConfidence { get; set; }
    /// <summary>Adresse supposée (réservé — jamais utilisée pour une campagne sans validation).</summary>
    public string? GuessedEmail { get; set; }

    // ── Coût / traçabilité ────────────────────────────────────────────────────
    public int SearchQueriesUsed { get; set; }
    public bool AutoApplied { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public Guid? ValidatedByUserId { get; set; }

    public Lead Lead { get; set; } = null!;
}
