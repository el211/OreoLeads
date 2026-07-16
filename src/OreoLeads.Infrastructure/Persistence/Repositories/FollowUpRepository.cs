using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.FollowUps.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Persistence.Repositories;

public class FollowUpRepository : IFollowUpRepository
{
    private readonly ApplicationDbContext _context;

    public FollowUpRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FollowUpDto>> GetByLeadIdAsync(Guid leadId, CancellationToken ct = default)
    {
        return await _context.FollowUps
            .AsNoTracking()
            .Include(f => f.Lead)
            .Where(f => f.LeadId == leadId)
            .OrderByDescending(f => f.ScheduledAt)
            .Select(f => MapToDto(f))
            .ToListAsync(ct);
    }

    public async Task<List<FollowUpDto>> GetPendingAsync(CancellationToken ct = default)
    {
        return await _context.FollowUps
            .AsNoTracking()
            .Include(f => f.Lead)
            .Where(f => f.Status == FollowUpStatus.Pending)
            .OrderBy(f => f.ScheduledAt)
            .Select(f => MapToDto(f))
            .ToListAsync(ct);
    }

    public async Task<List<FollowUpDto>> GetOverdueAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.FollowUps
            .AsNoTracking()
            .Include(f => f.Lead)
            .Where(f => f.Status == FollowUpStatus.Pending && f.ScheduledAt < now)
            .OrderBy(f => f.ScheduledAt)
            .Select(f => MapToDto(f))
            .ToListAsync(ct);
    }

    public async Task<FollowUp> CreateAsync(FollowUp followUp, CancellationToken ct = default)
    {
        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync(ct);
        return followUp;
    }

    public async Task UpdateAsync(FollowUp followUp, CancellationToken ct = default)
    {
        _context.FollowUps.Update(followUp);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var followUp = await _context.FollowUps.FindAsync([id], ct);
        if (followUp != null)
        {
            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<FollowUp?> GetEntityByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.FollowUps.FindAsync([id], ct);

    private static FollowUpDto MapToDto(FollowUp f) => new()
    {
        Id = f.Id,
        LeadId = f.LeadId,
        CompanyName = f.Lead?.CompanyName,
        ScheduledAt = f.ScheduledAt,
        UserId = f.UserId,
        UserName = f.UserName,
        Comment = f.Comment,
        Status = f.Status,
        StatusLabel = GetStatusLabel(f.Status),
        Priority = f.Priority,
        PriorityLabel = GetPriorityLabel(f.Priority),
        CompletedAt = f.CompletedAt,
        CreatedAt = f.CreatedAt,
        UpdatedAt = f.UpdatedAt
    };

    private static string GetStatusLabel(FollowUpStatus s) => s switch
    {
        FollowUpStatus.Pending => "En attente",
        FollowUpStatus.Done => "Terminé",
        FollowUpStatus.Cancelled => "Annulé",
        FollowUpStatus.Rescheduled => "Reprogrammé",
        _ => s.ToString()
    };

    private static string GetPriorityLabel(LeadPriority p) => p switch
    {
        LeadPriority.Low => "Basse",
        LeadPriority.Medium => "Moyenne",
        LeadPriority.High => "Haute",
        LeadPriority.Urgent => "Urgente",
        _ => p.ToString()
    };
}
