using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface IContactDiscoveryService
{
    Task<string> InspectAsync(Lead lead, CancellationToken ct = default);
}
