using OreoLeads.Application.Features.Tags.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface ITagRepository
{
    Task<List<TagDto>> GetAllAsync(CancellationToken ct = default);
    Task<Tag?> GetEntityByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tag> CreateAsync(Tag tag, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Tag> GetOrCreateAsync(string name, CancellationToken ct = default);
    Task AddTagToLeadAsync(Guid leadId, Guid tagId, CancellationToken ct = default);
    Task RemoveTagFromLeadAsync(Guid leadId, Guid tagId, CancellationToken ct = default);
}
