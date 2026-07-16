using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

public class SearchQuery : BaseEntity
{
    public string? UserId { get; set; }
    public string? Keywords { get; set; }
    public string? Region { get; set; }
    public string? Department { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Industry { get; set; }
    public string? NafCode { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public int MaxResults { get; set; } = 50;
    public string Provider { get; set; } = "OpenDataGouv";
    public int DurationMs { get; set; }
    public int TotalFound { get; set; }
    public int NewLeads { get; set; }
    public int UpdatedLeads { get; set; }
    public int Duplicates { get; set; }
    public int Errors { get; set; }
    public string Status { get; set; } = "Searched";
}
