namespace OreoLeads.Infrastructure.Enrichment;

public sealed class EnrichmentSettings
{
    public const string Section = "Enrichment";

    public bool AutoEnrichOnImport { get; set; } = true;
    /// <summary>Score minimal pour appliquer automatiquement un site/e-mail au lead.</summary>
    public double AutoApplyThreshold { get; set; } = 0.8;
    /// <summary>Score minimal pour proposer un candidat en revue manuelle (NeedsReview).</summary>
    public double ReviewThreshold { get; set; } = 0.4;
    public int MaxConcurrentJobs { get; set; } = 2;
    public int MaxJobsPerTick { get; set; } = 5;
    public int TickSeconds { get; set; } = 10;
    /// <summary>Nombre maximal de requêtes Brave par prospect (maîtrise du quota).</summary>
    public int MaxQueriesPerLead { get; set; } = 4;

    /// <summary>
    /// Domaines jamais considérés comme site officiel (annuaires, réseaux sociaux,
    /// plateformes de réservation, registres) mais conservés comme sources secondaires.
    /// </summary>
    public string[] DirectoryBlacklist { get; set; } =
    [
        "pagesjaunes.fr", "pappers.fr", "societe.com", "annuaire-entreprises.data.gouv.fr",
        "verif.com", "infogreffe.fr", "kompass.com", "google.com", "google.fr",
        "facebook.com", "instagram.com", "linkedin.com", "twitter.com", "x.com",
        "planity.com", "treatwell.fr", "thefork.fr", "lafourchette.fr", "booking.com",
        "tripadvisor.fr", "tripadvisor.com", "doctolib.fr", "yelp.fr", "yelp.com",
        "mappy.com", "leboncoin.fr", "wikipedia.org", "youtube.com", "pinterest.com",
        "ubereats.com", "deliveroo.fr", "justacote.com", "hoodspot.fr", "118712.fr",
        "annuaire.com", "figaro.fr", "lentreprise.com",
    ];

    // ── Catégorisation des sources secondaires ────────────────────────────────
    public static readonly string[] SocialDomains =
        ["facebook.com", "instagram.com", "linkedin.com", "twitter.com", "x.com", "youtube.com", "pinterest.com"];
    public static readonly string[] BookingDomains =
        ["planity.com", "treatwell.fr", "thefork.fr", "lafourchette.fr", "booking.com", "doctolib.fr", "calendly.com", "fresha.com", "zenchef.com"];
    public static readonly string[] LegalRegistryDomains =
        ["pappers.fr", "societe.com", "annuaire-entreprises.data.gouv.fr", "infogreffe.fr", "verif.com"];
}
