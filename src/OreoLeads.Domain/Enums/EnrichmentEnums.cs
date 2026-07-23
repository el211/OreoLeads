namespace OreoLeads.Domain.Enums;

public enum EnrichmentStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    NeedsReview = 3,
    Failed = 4,
}

public enum DiscoveredEmailKind
{
    Unknown = 0,
    Generic = 1,    // contact@, info@, bonjour@…
    Nominative = 2, // prenom.nom@…
    Other = 3,
}
