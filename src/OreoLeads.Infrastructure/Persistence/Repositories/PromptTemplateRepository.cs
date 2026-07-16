using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence.Repositories;

internal sealed class PromptTemplateRepository : IPromptTemplateRepository
{
    private readonly ApplicationDbContext _db;

    public PromptTemplateRepository(ApplicationDbContext db) => _db = db;

    public async Task<IList<PromptTemplate>> GetAllAsync()
        => await _db.Set<PromptTemplate>()
            .OrderBy(t => t.Key)
            .ToListAsync();

    public async Task<PromptTemplate?> GetByKeyAsync(string key)
        => await _db.Set<PromptTemplate>()
            .FirstOrDefaultAsync(t => t.Key == key);

    public async Task<PromptTemplate> UpsertAsync(PromptTemplate template)
    {
        var existing = await GetByKeyAsync(template.Key);
        if (existing is null)
        {
            _db.Set<PromptTemplate>().Add(template);
        }
        else
        {
            existing.Name        = template.Name;
            existing.Content     = template.Content;
            existing.Description = template.Description;
            existing.EmailType   = template.EmailType;
            existing.IsSystem    = template.IsSystem;
            existing.SetUpdatedAt();
        }

        await _db.SaveChangesAsync();
        return existing ?? template;
    }
}
