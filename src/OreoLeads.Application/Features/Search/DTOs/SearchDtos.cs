namespace OreoLeads.Application.Features.Search.DTOs;

public class CompanySearchRequestDto
{
    public string? Keywords { get; set; }
    public string? Region { get; set; }
    public string? Department { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Industry { get; set; }
    public string? NafCode { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public int MaxResults { get; set; } = 50;

    // ── Filtres entrepreneurs individuels / petites structures ───────────────
    public bool IncludeIndividualEntrepreneurs { get; set; } = true;
    public bool OnlyIndividualEntrepreneurs { get; set; }
    public bool IncludeNoEmployees { get; set; } = true;
    public bool IncludeSecondaryEstablishments { get; set; }
    public List<string>? NatureJuridiqueCodes { get; set; }
    public DateOnly? CreatedAfter { get; set; }
    public DateOnly? CreatedBefore { get; set; }
}

public class CompanySearchResultDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string? Siren { get; set; }
    public string? Siret { get; set; }
    public string? Industry { get; set; }
    public string? NafCode { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Department { get; set; }
    public string? Region { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? EmployeeCount { get; set; }
    public bool IsActive { get; set; } = true;
    public string Provider { get; set; } = string.Empty;
    public bool AlreadyExists { get; set; }
    public Guid? ExistingLeadId { get; set; }

    // ── Entrepreneurs individuels ─────────────────────────────────────────────
    public string? TradeName { get; set; }
    public bool IsIndividualEntrepreneur { get; set; }
    public string? EntrepreneurFirstName { get; set; }
    public string? EntrepreneurLastName { get; set; }
    public string? NatureJuridique { get; set; }
    public DateOnly? CreationDate { get; set; }
    public bool IsNonDiffusible { get; set; }
}

public class CompanySearchResponseDto
{
    public Guid SearchId { get; set; }
    public int TotalFound { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public List<CompanySearchResultDto> Results { get; set; } = new();
    public string Provider { get; set; } = string.Empty;
    public int DurationMs { get; set; }
}

public class SearchImportRequestDto
{
    public Guid? SearchId { get; set; }
    public List<CompanySearchResultDto> Companies { get; set; } = new();
}

public class SearchImportResultDto
{
    public int NewLeads { get; set; }
    public int UpdatedLeads { get; set; }
    public int Duplicates { get; set; }
    public int Errors { get; set; }
    public List<Guid> NewLeadIds { get; set; } = new();
}

public class SearchHistoryDto
{
    public Guid Id { get; set; }
    public string? Keywords { get; set; }
    public string? Region { get; set; }
    public string? Department { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Industry { get; set; }
    public string? NafCode { get; set; }
    public bool ActiveOnly { get; set; }
    public int MaxResults { get; set; }
    public string Provider { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public int TotalFound { get; set; }
    public int NewLeads { get; set; }
    public int UpdatedLeads { get; set; }
    public int Duplicates { get; set; }
    public int Errors { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
