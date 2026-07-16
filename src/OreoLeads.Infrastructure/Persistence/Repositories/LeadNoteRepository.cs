using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Features.LeadNotes.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Repositories;

public class LeadNoteRepository
{
    private readonly ApplicationDbContext _context;

    public LeadNoteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeadNoteDto>> GetByLeadIdAsync(Guid leadId, CancellationToken ct = default)
    {
        return await _context.LeadNotes
            .AsNoTracking()
            .Where(n => n.LeadId == leadId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new LeadNoteDto
            {
                Id = n.Id,
                LeadId = n.LeadId,
                Title = n.Title,
                Content = n.Content,
                AuthorId = n.AuthorId,
                AuthorName = n.AuthorName,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<LeadNote> CreateAsync(LeadNote note, CancellationToken ct = default)
    {
        _context.LeadNotes.Add(note);
        await _context.SaveChangesAsync(ct);
        return note;
    }

    public async Task<LeadNote?> GetEntityByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.LeadNotes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task UpdateAsync(LeadNote note, CancellationToken ct = default)
    {
        _context.LeadNotes.Update(note);
        await _context.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var note = await GetEntityByIdAsync(id, ct);
        if (note != null)
        {
            note.IsDeleted = true;
            note.SetUpdatedAt();
            _context.LeadNotes.Update(note);
            await _context.SaveChangesAsync(ct);
        }
    }
}
