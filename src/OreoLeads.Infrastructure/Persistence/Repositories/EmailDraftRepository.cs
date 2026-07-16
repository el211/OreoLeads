using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Common.Models;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Persistence.Repositories;

internal sealed class EmailDraftRepository : IEmailDraftRepository
{
    private readonly ApplicationDbContext _db;

    public EmailDraftRepository(ApplicationDbContext db) => _db = db;

    public async Task<GeneratedEmail?> GetByIdAsync(Guid id)
        => await _db.GeneratedEmails
            .Include(e => e.Lead)
            .Include(e => e.Versions)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<PagedResult<GeneratedEmail>> GetAllAsync(int page, int pageSize, string? statusFilter = null)
    {
        var query = _db.GeneratedEmails
            .Include(e => e.Lead)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter)
            && Enum.TryParse<EmailStatus>(statusFilter, true, out var status))
        {
            query = query.Where(e => e.Status == status);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<GeneratedEmail> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<IList<GeneratedEmail>> GetByLeadIdAsync(Guid leadId)
        => await _db.GeneratedEmails
            .Where(e => e.LeadId == leadId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

    public async Task<GeneratedEmail> CreateAsync(GeneratedEmail draft)
    {
        _db.GeneratedEmails.Add(draft);
        await _db.SaveChangesAsync();
        return draft;
    }

    public async Task<GeneratedEmail> UpdateAsync(GeneratedEmail draft)
    {
        _db.GeneratedEmails.Update(draft);
        await _db.SaveChangesAsync();
        return draft;
    }

    public async Task<EmailDraftVersion> AddVersionAsync(EmailDraftVersion version)
    {
        _db.Set<EmailDraftVersion>().Add(version);
        await _db.SaveChangesAsync();
        return version;
    }

    public async Task<IList<EmailDraftVersion>> GetVersionsAsync(Guid draftId)
        => await _db.Set<EmailDraftVersion>()
            .Where(v => v.EmailDraftId == draftId)
            .OrderBy(v => v.Version)
            .ToListAsync();
}
