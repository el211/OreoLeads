using OreoLeads.Application.Common.Models;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface IEmailDraftRepository
{
    Task<GeneratedEmail?> GetByIdAsync(Guid id);
    Task<PagedResult<GeneratedEmail>> GetAllAsync(int page, int pageSize, string? statusFilter = null);
    Task<IList<GeneratedEmail>> GetByLeadIdAsync(Guid leadId);
    Task<GeneratedEmail> CreateAsync(GeneratedEmail draft);
    Task<GeneratedEmail> UpdateAsync(GeneratedEmail draft);
    Task<EmailDraftVersion> AddVersionAsync(EmailDraftVersion version);
    Task<IList<EmailDraftVersion>> GetVersionsAsync(Guid draftId);
}
