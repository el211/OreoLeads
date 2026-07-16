using OreoLeads.Application.Features.Leads.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface IExcelExportService
{
    Task<byte[]> ExportLeadsToCsvAsync(List<LeadSummaryDto> leads, CancellationToken ct = default);
    Task<byte[]> ExportLeadsToExcelAsync(List<LeadSummaryDto> leads, CancellationToken ct = default);
}
