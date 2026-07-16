using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.LeadActivities.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Persistence.Repositories;

public class LeadActivityRepository : ILeadActivityRepository
{
    private readonly ApplicationDbContext _context;

    public LeadActivityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeadActivityDto>> GetByLeadIdAsync(Guid leadId, CancellationToken ct = default)
    {
        return await _context.LeadActivities
            .AsNoTracking()
            .Where(a => a.LeadId == leadId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new LeadActivityDto
            {
                Id = a.Id,
                LeadId = a.LeadId,
                Type = a.Type,
                TypeLabel = GetTypeLabel(a.Type),
                Description = a.Description,
                UserId = a.UserId,
                Metadata = a.Metadata,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<LeadActivity> AddAsync(LeadActivity activity, CancellationToken ct = default)
    {
        _context.LeadActivities.Add(activity);
        await _context.SaveChangesAsync(ct);
        return activity;
    }

    private static string GetTypeLabel(ActivityType type) => type switch
    {
        ActivityType.Created => "Création",
        ActivityType.Updated => "Mise à jour",
        ActivityType.StatusChanged => "Changement de statut",
        ActivityType.EmailGenerated => "Email généré",
        ActivityType.EmailSent => "Email envoyé",
        ActivityType.PhoneCall => "Appel téléphonique",
        ActivityType.Meeting => "Rendez-vous",
        ActivityType.WebsiteAnalyzed => "Site analysé",
        ActivityType.NoteAdded => "Note ajoutée",
        ActivityType.NoteUpdated => "Note modifiée",
        ActivityType.NoteDeleted => "Note supprimée",
        ActivityType.FollowUpCreated => "Relance créée",
        ActivityType.Import => "Import",
        ActivityType.Export => "Export",
        _ => type.ToString()
    };
}
