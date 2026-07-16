using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Tags.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Repositories;

public class TagRepository : ITagRepository
{
    private readonly ApplicationDbContext _context;

    public TagRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TagDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TagDto { Id = t.Id, Name = t.Name, Color = t.Color })
            .ToListAsync(ct);
    }

    public async Task<Tag?> GetEntityByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Tags.FindAsync([id], ct);

    public async Task<Tag> CreateAsync(Tag tag, CancellationToken ct = default)
    {
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync(ct);
        return tag;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tag = await _context.Tags.FindAsync([id], ct);
        if (tag != null)
        {
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<Tag> GetOrCreateAsync(string name, CancellationToken ct = default)
    {
        var existing = await _context.Tags.FirstOrDefaultAsync(t => t.Name == name, ct);
        if (existing != null) return existing;

        var tag = new Tag { Name = name };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync(ct);
        return tag;
    }

    public async Task AddTagToLeadAsync(Guid leadId, Guid tagId, CancellationToken ct = default)
    {
        var exists = await _context.LeadTags
            .AnyAsync(lt => lt.LeadId == leadId && lt.TagId == tagId, ct);

        if (!exists)
        {
            _context.LeadTags.Add(new LeadTag { LeadId = leadId, TagId = tagId });
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveTagFromLeadAsync(Guid leadId, Guid tagId, CancellationToken ct = default)
    {
        var leadTag = await _context.LeadTags
            .FirstOrDefaultAsync(lt => lt.LeadId == leadId && lt.TagId == tagId, ct);

        if (leadTag != null)
        {
            _context.LeadTags.Remove(leadTag);
            await _context.SaveChangesAsync(ct);
        }
    }
}
