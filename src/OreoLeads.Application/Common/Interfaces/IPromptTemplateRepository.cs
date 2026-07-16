using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface IPromptTemplateRepository
{
    Task<IList<PromptTemplate>> GetAllAsync();
    Task<PromptTemplate?> GetByKeyAsync(string key);
    Task<PromptTemplate> UpsertAsync(PromptTemplate template);
}
