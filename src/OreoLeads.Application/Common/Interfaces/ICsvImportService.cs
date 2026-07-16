using OreoLeads.Application.Features.Leads.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface ICsvImportService
{
    Task<List<ImportLeadDto>> ParseAsync(Stream stream, CancellationToken ct = default);
    Lead MapToLead(ImportLeadDto dto);
}
