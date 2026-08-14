using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Common.Models;
using OreoLeads.Application.Features.Leads.DTOs;
using OreoLeads.Application.Features.Tags.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Persistence.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly ApplicationDbContext _context;

    public LeadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LeadSummaryDto>> SearchAsync(LeadFilterDto filter, CancellationToken ct = default)
    {
        var query = BuildQuery(filter);

        var totalCount = await query.CountAsync(ct);

        query = ApplySorting(query, filter);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(l => MapToSummary(l))
            .ToListAsync(ct);

        // Date du dernier e-mail envoyé pour la page courante (une seule requête).
        var pageIds = items.Select(i => i.Id).ToList();
        if (pageIds.Count > 0)
        {
            var lastSent = await _context.EmailSendJobs
                .Where(j => pageIds.Contains(j.LeadId) && j.SentAt != null)
                .GroupBy(j => j.LeadId)
                .Select(g => new { LeadId = g.Key, Last = g.Max(j => j.SentAt) })
                .ToDictionaryAsync(x => x.LeadId, x => x.Last, ct);

            foreach (var item in items)
                if (lastSent.TryGetValue(item.Id, out var last))
                    item.LastEmailSentAt = last;
        }

        return new PagedResult<LeadSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<LeadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var lead = await _context.Leads
            .AsNoTracking()
            .Include(l => l.LeadTags).ThenInclude(lt => lt.Tag)
            .Include(l => l.Notes)
            .Include(l => l.Activities)
            .Include(l => l.FollowUps)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        return lead == null ? null : MapToDto(lead);
    }

    public async Task<Lead> CreateAsync(Lead lead, CancellationToken ct = default)
    {
        _context.Leads.Add(lead);
        await _context.SaveChangesAsync(ct);
        return lead;
    }

    public async Task UpdateAsync(Lead lead, CancellationToken ct = default)
    {
        _context.Leads.Update(lead);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var lead = await _context.Leads.FindAsync([id], ct);
        if (lead != null)
        {
            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.Leads.AnyAsync(l => l.Id == id, ct);

    // « Non contacté » = aucun EmailSendJob réellement envoyé (SentAt renseigné).
    private IQueryable<Lead> UncontactedQuery()
        => _context.Leads.Where(l =>
            !_context.EmailSendJobs.Any(j => j.LeadId == l.Id && j.SentAt != null));

    public async Task<int> CountUncontactedAsync(CancellationToken ct = default)
        => await UncontactedQuery().CountAsync(ct);

    public async Task<int> DeleteUncontactedAsync(CancellationToken ct = default)
    {
        // On charge puis Remove (comme la suppression unitaire) pour laisser jouer
        // le cascade des entités liées (activités, notes, envois, enrichissements…).
        var toDelete = await UncontactedQuery().ToListAsync(ct);
        if (toDelete.Count == 0) return 0;

        _context.Leads.RemoveRange(toDelete);
        await _context.SaveChangesAsync(ct);
        return toDelete.Count;
    }

    public async Task<Lead?> GetEntityByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Leads
            .Include(l => l.LeadTags)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<List<LeadSummaryDto>> GetByFilterForExportAsync(LeadFilterDto filter, CancellationToken ct = default)
    {
        var query = BuildQuery(filter);
        query = ApplySorting(query, filter);
        return await query.Select(l => MapToSummary(l)).ToListAsync(ct);
    }

    public async Task<int> BulkImportAsync(List<Lead> leads, CancellationToken ct = default)
    {
        _context.Leads.AddRange(leads);
        return await _context.SaveChangesAsync(ct);
    }

    public async Task<int> BulkUpdateIndustryAsync(
        IReadOnlyCollection<Guid> ids, string? industry, CancellationToken ct = default)
    {
        if (ids.Count == 0) return 0;
        var value = string.IsNullOrWhiteSpace(industry) ? null : industry.Trim();

        // ExecuteUpdate : une seule requête SQL, respecte le filtre multi-tenant.
        return await _context.Leads
            .Where(l => ids.Contains(l.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(l => l.Industry, value)
                .SetProperty(l => l.UpdatedAt, DateTime.UtcNow), ct);
    }

    private IQueryable<Lead> BuildQuery(LeadFilterDto filter)
    {
        var query = _context.Leads
            .AsNoTracking()
            .Include(l => l.LeadTags).ThenInclude(lt => lt.Tag)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(l =>
                l.CompanyName.ToLower().Contains(search) ||
                (l.TradeName != null && l.TradeName.ToLower().Contains(search)) ||
                (l.City != null && l.City.ToLower().Contains(search)) ||
                (l.Industry != null && l.Industry.ToLower().Contains(search)) ||
                (l.Email != null && l.Email.ToLower().Contains(search)) ||
                (l.Siren != null && l.Siren.Contains(search)) ||
                (l.Siret != null && l.Siret.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
            query = query.Where(l => l.City != null && l.City.ToLower().Contains(filter.City.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.Industry))
            query = query.Where(l => l.Industry != null && l.Industry.ToLower().Contains(filter.Industry.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.LegalForm))
        {
            // Match whole-word: pad both sides with a space so "SA" doesn't match "SARL"
            var lf = filter.LegalForm.ToUpper();
            var padded = " " + lf + " ";
            query = query.Where(l =>
                (" " + l.CompanyName.ToUpper() + " ").Contains(padded));
        }

        if (filter.Status.HasValue)
            query = query.Where(l => l.Status == filter.Status.Value);

        if (filter.Priority.HasValue)
            query = query.Where(l => l.Priority == filter.Priority.Value);

        if (filter.MinScore.HasValue)
            query = query.Where(l => l.Score >= filter.MinScore.Value);

        if (filter.MaxScore.HasValue)
            query = query.Where(l => l.Score <= filter.MaxScore.Value);

        if (filter.HasWebsite.HasValue)
            query = filter.HasWebsite.Value
                ? query.Where(l => l.Website != null && l.Website != string.Empty)
                : query.Where(l => l.Website == null || l.Website == string.Empty);

        if (filter.HasEmail.HasValue)
            query = filter.HasEmail.Value
                ? query.Where(l => l.Email != null && l.Email != string.Empty)
                : query.Where(l => l.Email == null || l.Email == string.Empty);

        if (filter.HasPhone.HasValue)
            query = filter.HasPhone.Value
                ? query.Where(l => l.Phone != null && l.Phone != string.Empty)
                : query.Where(l => l.Phone == null || l.Phone == string.Empty);

        if (filter.HasMobilePhone.HasValue)
        {
            // Portables FR : 06…, 07…, +336…, +337…, 00336…, 00337…
            if (filter.HasMobilePhone.Value)
                query = query.Where(l =>
                    l.Phone != null && (
                        l.Phone.StartsWith("06")    ||
                        l.Phone.StartsWith("07")    ||
                        l.Phone.StartsWith("+336")  ||
                        l.Phone.StartsWith("+337")  ||
                        l.Phone.StartsWith("00336") ||
                        l.Phone.StartsWith("00337")));
            else
                // Fixe = a un téléphone mais ce n'est pas un portable
                query = query.Where(l =>
                    l.Phone != null && l.Phone != string.Empty &&
                    !l.Phone.StartsWith("06")    &&
                    !l.Phone.StartsWith("07")    &&
                    !l.Phone.StartsWith("+336")  &&
                    !l.Phone.StartsWith("+337")  &&
                    !l.Phone.StartsWith("00336") &&
                    !l.Phone.StartsWith("00337"));
        }

        if (filter.HasContact == true)
            query = query.Where(l =>
                (l.Phone != null && l.Phone != string.Empty) ||
                (l.Email != null && l.Email != string.Empty));

        if (filter.TagId.HasValue)
            query = query.Where(l => l.LeadTags.Any(lt => lt.TagId == filter.TagId.Value));

        // Candidats à relance : dernier e-mail envoyé il y a au moins N jours.
        if (filter.MinDaysSinceEmail is > 0)
        {
            var cutoff = DateTime.UtcNow.AddDays(-filter.MinDaysSinceEmail.Value);
            query = query.Where(l => _context.EmailSendJobs
                .Where(j => j.LeadId == l.Id && j.SentAt != null)
                .Max(j => (DateTime?)j.SentAt) <= cutoff);
        }

        return query;
    }

    // Date plancher UTC pour placer en dernier les leads sans e-mail envoyé lors du tri.
    private static readonly DateTime Epoch = new(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private IQueryable<Lead> ApplySorting(IQueryable<Lead> query, LeadFilterDto filter)
    {
        return filter.SortBy?.ToLower() switch
        {
            "score" => filter.SortDesc ? query.OrderByDescending(l => l.Score) : query.OrderBy(l => l.Score),
            "companyname" => filter.SortDesc ? query.OrderByDescending(l => l.CompanyName) : query.OrderBy(l => l.CompanyName),
            "city" => filter.SortDesc ? query.OrderByDescending(l => l.City) : query.OrderBy(l => l.City),
            "status" => filter.SortDesc ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status),
            "priority" => filter.SortDesc ? query.OrderByDescending(l => l.Priority) : query.OrderBy(l => l.Priority),
            // « Dernière modification » : retombe sur la date de création si jamais modifié.
            "updatedat" => filter.SortDesc
                ? query.OrderByDescending(l => l.UpdatedAt ?? l.CreatedAt)
                : query.OrderBy(l => l.UpdatedAt ?? l.CreatedAt),
            // « Dernier e-mail envoyé » : max des SentAt ; les leads sans envoi passent en dernier.
            "lastemailsent" => filter.SortDesc
                ? query.OrderByDescending(l => _context.EmailSendJobs
                    .Where(j => j.LeadId == l.Id && j.SentAt != null)
                    .Max(j => (DateTime?)j.SentAt) ?? Epoch)
                : query.OrderBy(l => _context.EmailSendJobs
                    .Where(j => j.LeadId == l.Id && j.SentAt != null)
                    .Max(j => (DateTime?)j.SentAt) ?? Epoch),
            _ => filter.SortDesc ? query.OrderByDescending(l => l.CreatedAt) : query.OrderByDescending(l => l.CreatedAt)
        };
    }

    private static LeadSummaryDto MapToSummary(Lead l) => new()
    {
        Id = l.Id,
        CompanyName = l.CompanyName,
        TradeName = l.TradeName,
        Industry = l.Industry,
        City = l.City,
        Department = l.Department,
        Region = l.Region,
        Email = l.Email,
        Phone = l.Phone,
        Website = l.Website,
        Status = l.Status,
        StatusLabel = GetStatusLabel(l.Status),
        Priority = l.Priority,
        PriorityLabel = GetPriorityLabel(l.Priority),
        Score = l.Score,
        Tags = l.LeadTags.Select(lt => new TagDto
        {
            Id = lt.Tag.Id,
            Name = lt.Tag.Name,
            Color = lt.Tag.Color
        }).ToList(),
        CreatedAt = l.CreatedAt,
        UpdatedAt = l.UpdatedAt
    };

    private static LeadDto MapToDto(Lead l) => new()
    {
        Id = l.Id,
        CompanyName = l.CompanyName,
        TradeName = l.TradeName,
        Industry = l.Industry,
        Description = l.Description,
        Website = l.Website,
        Email = l.Email,
        Phone = l.Phone,
        Address = l.Address,
        PostalCode = l.PostalCode,
        City = l.City,
        Department = l.Department,
        Region = l.Region,
        Country = l.Country,
        Latitude = l.Latitude,
        Longitude = l.Longitude,
        Siren = l.Siren,
        Siret = l.Siret,
        NafCode = l.NafCode,
        EmployeeCount = l.EmployeeCount,
        Status = l.Status,
        StatusLabel = GetStatusLabel(l.Status),
        Priority = l.Priority,
        PriorityLabel = GetPriorityLabel(l.Priority),
        Score = l.Score,
        AssignedUserId = l.AssignedUserId,
        Tags = l.LeadTags.Select(lt => new TagDto
        {
            Id = lt.Tag.Id,
            Name = lt.Tag.Name,
            Color = lt.Tag.Color
        }).ToList(),
        NotesCount = l.Notes.Count,
        ActivitiesCount = l.Activities.Count,
        FollowUpsCount = l.FollowUps.Count,
        CreatedAt = l.CreatedAt,
        UpdatedAt = l.UpdatedAt
    };

    internal static string GetStatusLabel(LeadStatus status) => status switch
    {
        LeadStatus.New => "Nouveau",
        LeadStatus.Qualified => "Qualifié",
        LeadStatus.ReadyToContact => "Prêt à contacter",
        LeadStatus.EmailPrepared => "Email préparé",
        LeadStatus.EmailSent => "Email envoyé",
        LeadStatus.FollowUp1 => "Relance 1",
        LeadStatus.FollowUp2 => "Relance 2",
        LeadStatus.Meeting => "Rendez-vous",
        LeadStatus.ProposalSent => "Devis envoyé",
        LeadStatus.Client => "Client",
        LeadStatus.Rejected => "Refusé",
        LeadStatus.DoNotContact => "Ne pas contacter",
        LeadStatus.SmsSent => "SMS envoyé",
        _ => status.ToString()
    };

    internal static string GetPriorityLabel(LeadPriority priority) => priority switch
    {
        LeadPriority.Low => "Basse",
        LeadPriority.Medium => "Moyenne",
        LeadPriority.High => "Haute",
        LeadPriority.Urgent => "Urgente",
        _ => priority.ToString()
    };

    // ── Meta ──────────────────────────────────────────────────────────────────

    private static readonly string[] KnownLegalForms =
    [
        "SARL", "SAS", "SASU", "EURL", "SA", "EI", "EIRL",
        "SNC", "SCOP", "SCA", "SCS", "GIE", "ASSOCIATION", "ASSO"
    ];

    public async Task<LeadsMetaDto> GetMetaAsync(CancellationToken ct = default)
    {
        // Distinct non-empty industries ordered by frequency
        var industries = await _context.Leads
            .Where(l => l.Industry != null && l.Industry != "")
            .GroupBy(l => l.Industry!)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(200)
            .ToListAsync(ct);

        // Which known legal forms actually appear in the data?
        var companyNames = await _context.Leads
            .Select(l => l.CompanyName.ToUpper())
            .ToListAsync(ct);

        var legalForms = KnownLegalForms
            .Where(form =>
            {
                var padded = " " + form + " ";
                return companyNames.Any(n => (" " + n + " ").Contains(padded));
            })
            .ToList();

        return new LeadsMetaDto { Industries = industries, LegalForms = legalForms };
    }
}
