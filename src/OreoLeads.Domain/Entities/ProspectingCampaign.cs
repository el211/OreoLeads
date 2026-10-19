using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

public class ProspectingCampaign : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public string CriteriaJson { get; set; } = "{}";
    public string Status { get; set; } = "Paused";
    public int DailyLimit { get; set; } = 100;
    public int DailyPageLimit { get; set; } = 20;
    public int MinimumScore { get; set; } = 75;
    public int NextPage { get; set; } = 1;
    public int PageOffset { get; set; }
    public DateTime NextRunAt { get; set; } = DateTime.UtcNow;
    public DateTime QuotaDate { get; set; } = DateTime.UtcNow.Date;
    public int DiscoveredToday { get; set; }
    public int PagesToday { get; set; }
    public int Failures { get; set; }
    public string? LastError { get; set; }
    public int RetentionDays { get; set; } = 90;
    public int DiscoveryIntervalMinutes { get; set; } = 1;
    public DateTime NextDiscoveryAt { get; set; } = DateTime.UtcNow;
    public int MaxPendingEnrichments { get; set; } = 20;
    public bool AutomaticAiRecommendation { get; set; }
}

public class CampaignProspect : BaseEntity
{
    public Guid CampaignId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid LeadId { get; set; }
    public string State { get; set; } = "Enriching";
    public int Score { get; set; }
    public string EvidenceJson { get; set; } = "[]";
    public DateTime? QualifiedAt { get; set; }
    public string? RecommendationJson { get; set; }
    public int AiAttempts { get; set; }
    public DateTime? NextAiAttemptAt { get; set; }
    public string? AiError { get; set; }
    public DateTime? AiLeaseUntil { get; set; }
    public Guid? AiLeaseToken { get; set; }
}

public class CampaignExecution : BaseEntity
{
    public Guid CampaignId { get; set; }
    public Guid OrganizationId { get; set; }
    public int Page { get; set; }
    public int Discovered { get; set; }
    public int Duplicates { get; set; }
    public string Outcome { get; set; } = "Completed";
    public string? Error { get; set; }
}
