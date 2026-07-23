namespace OreoLeads.Application.Features.Enrichment.DTOs;

public sealed record WebsiteCandidateDto(string Url, double Score, string Category, List<string> Signals);
public sealed record ExternalProfileDto(string Url, string Category);

public sealed record LeadEnrichmentDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string Status { get; init; } = "";
    public DateTime ScheduledAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int AttemptCount { get; init; }
    public string? ErrorMessage { get; init; }

    // Site
    public string? ChosenWebsiteUrl { get; init; }
    public double? WebsiteConfidence { get; init; }
    public List<string> MatchedSignals { get; init; } = new();
    public List<WebsiteCandidateDto> Candidates { get; init; } = new();
    public List<ExternalProfileDto> ExternalProfiles { get; init; } = new();
    public bool AutoApplied { get; init; }

    // E-mail
    public string? DiscoveredEmail { get; init; }
    public string? EmailSourceUrl { get; init; }
    public string? EmailSourceType { get; init; }
    public string EmailKind { get; init; } = "";
    public double? EmailConfidence { get; init; }
    public string? GuessedEmail { get; init; }

    public int SearchQueriesUsed { get; init; }
    public DateTime? ValidatedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Validation manuelle d'un enrichissement : applique le site et/ou l'e-mail choisi
/// au lead et pose le verrou de validation (non écrasable par l'automatique).
/// </summary>
public sealed record EnrichmentValidateRequestDto
{
    public string? Website { get; init; }
    public string? Email { get; init; }
    public bool AcceptWebsite { get; init; }
    public bool AcceptEmail { get; init; }
}

public sealed record EnrichmentQueueResultDto(Guid EnrichmentId, string Status);
