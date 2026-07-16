using OreoLeads.Application.Features.Leads.DTOs;

namespace OreoLeads.Application.Features.Dashboard.DTOs;

public class DashboardStatsDto
{
    public int TotalLeads { get; init; }
    public int NewLeads { get; init; }
    public int Clients { get; init; }
    public int EmailsSent { get; init; }
    public int PendingFollowUps { get; init; }
    public int LeadsThisMonth { get; init; }
    public List<StatusDistributionDto> StatusDistribution { get; init; } = new();
    public List<IndustryDistributionDto> IndustryDistribution { get; init; } = new();
    public List<CityDistributionDto> CityDistribution { get; init; } = new();
    public List<LeadSummaryDto> RecentLeads { get; init; } = new();
}

public class StatusDistributionDto
{
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public int Count { get; init; }
}

public class IndustryDistributionDto
{
    public string Industry { get; init; } = string.Empty;
    public int Count { get; init; }
}

public class CityDistributionDto
{
    public string City { get; init; } = string.Empty;
    public int Count { get; init; }
}
