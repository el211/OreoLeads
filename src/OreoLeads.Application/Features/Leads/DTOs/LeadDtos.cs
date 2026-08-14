using OreoLeads.Application.Features.Tags.DTOs;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Application.Features.Leads.DTOs;

public class LeadDto
{
    public Guid Id { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string? TradeName { get; init; }
    public string? Industry { get; init; }
    public string? Description { get; init; }
    public string? Website { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? Department { get; init; }
    public string? Region { get; init; }
    public string Country { get; init; } = "France";
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? Siren { get; init; }
    public string? Siret { get; init; }
    public string? NafCode { get; init; }
    public int? EmployeeCount { get; init; }
    public LeadStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public LeadPriority Priority { get; init; }
    public string PriorityLabel { get; init; } = string.Empty;
    public int Score { get; init; }
    public Guid? AssignedUserId { get; init; }
    public List<TagDto> Tags { get; init; } = new();
    public int NotesCount { get; init; }
    public int ActivitiesCount { get; init; }
    public int FollowUpsCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class LeadSummaryDto
{
    public Guid Id { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string? TradeName { get; init; }
    public string? Industry { get; init; }
    public string? City { get; init; }
    public string? Department { get; init; }
    public string? Region { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public LeadStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public LeadPriority Priority { get; init; }
    public string PriorityLabel { get; init; } = string.Empty;
    public int Score { get; init; }
    public List<TagDto> Tags { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    /// <summary>Date du dernier e-mail réellement envoyé (max SentAt), ou null.</summary>
    public DateTime? LastEmailSentAt { get; set; }
}

public class CreateLeadDto
{
    public string CompanyName { get; init; } = string.Empty;
    public string? TradeName { get; init; }
    public string? Industry { get; init; }
    public string? Description { get; init; }
    public string? Website { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? Department { get; init; }
    public string? Region { get; init; }
    public string Country { get; init; } = "France";
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? Siren { get; init; }
    public string? Siret { get; init; }
    public string? NafCode { get; init; }
    public int? EmployeeCount { get; init; }
    public LeadStatus Status { get; init; } = LeadStatus.New;
    public LeadPriority Priority { get; init; } = LeadPriority.Medium;
    public int Score { get; init; } = 0;
    public Guid? AssignedUserId { get; init; }
    public List<Guid> TagIds { get; init; } = new();
}

public class UpdateLeadDto
{
    public string CompanyName { get; init; } = string.Empty;
    public string? TradeName { get; init; }
    public string? Industry { get; init; }
    public string? Description { get; init; }
    public string? Website { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? Department { get; init; }
    public string? Region { get; init; }
    public string Country { get; init; } = "France";
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? Siren { get; init; }
    public string? Siret { get; init; }
    public string? NafCode { get; init; }
    public int? EmployeeCount { get; init; }
    public LeadStatus Status { get; init; }
    public LeadPriority Priority { get; init; }
    public int Score { get; init; }
    public Guid? AssignedUserId { get; init; }
    public List<Guid> TagIds { get; init; } = new();
}

public class LeadFilterDto
{
    public string? Search { get; init; }
    public string? City { get; init; }
    public string? Industry { get; init; }
    public string? LegalForm { get; init; }
    public LeadStatus? Status { get; init; }
    public LeadPriority? Priority { get; init; }
    public int? MinScore { get; init; }
    public int? MaxScore { get; init; }
    public bool? HasWebsite { get; init; }
    public bool? HasEmail { get; init; }
    public bool? HasPhone { get; init; }
    /// <summary>Portable uniquement (06/07/+336/+337). false = fixe uniquement.</summary>
    public bool? HasMobilePhone { get; init; }
    /// <summary>Ne garde que les prospects qui ont un téléphone ou un email (au moins un moyen de contact).</summary>
    public bool? HasContact { get; init; }
    public Guid? TagId { get; init; }
    /// <summary>Ne garde que les prospects dont le dernier e-mail envoyé date d'au moins N jours (candidats à relance).</summary>
    public int? MinDaysSinceEmail { get; init; }
    public string? SortBy { get; init; }
    public bool SortDesc { get; init; } = false;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>Édition en masse du secteur d'activité pour plusieurs prospects.</summary>
public class BulkUpdateIndustryDto
{
    public List<Guid> LeadIds { get; init; } = new();
    public string? Industry { get; init; }
}

/// <summary>Remplissage automatique du secteur via l'IA pour plusieurs prospects.</summary>
public class BulkAutofillIndustryDto
{
    public List<Guid> LeadIds { get; init; } = new();
}

public class LeadsMetaDto
{
    public List<string> Industries { get; init; } = new();
    public List<string> LegalForms { get; init; } = new();
}

public class ImportLeadDto
{
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Industry { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? Siren { get; set; }
    public string? Siret { get; set; }
}
