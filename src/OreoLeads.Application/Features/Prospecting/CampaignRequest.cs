using System.ComponentModel.DataAnnotations;
using OreoLeads.Application.Features.Search.DTOs;

namespace OreoLeads.Application.Features.Prospecting;

public class CampaignRequest
{
    [Required, StringLength(200, MinimumLength = 3)] public string Name { get; set; } = "";
    [Required] public CompanySearchRequestDto Criteria { get; set; } = new();
    [Range(1, 1000)] public int DailyLimit { get; set; } = 100;
    [Range(1, 100)] public int DailyPageLimit { get; set; } = 20;
    [Range(0, 100)] public int MinimumScore { get; set; } = 75;
    [Range(7, 365)] public int RetentionDays { get; set; } = 90;
    [Range(1, 10080)] public int DiscoveryIntervalMinutes { get; set; } = 1;
    [Range(1, 1000)] public int MaxPendingEnrichments { get; set; } = 20;
    public bool AutomaticAiRecommendation { get; set; }
}
