using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/analytics/reports")]
[Authorize]
public class AnalyticsReportController : ControllerBase
{
    private readonly IReportService _reportSvc;
    private readonly ICurrentUserService _currentUser;

    public AnalyticsReportController(IReportService reportSvc, ICurrentUserService currentUser)
    {
        _reportSvc = reportSvc;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetReports(CancellationToken ct)
    {
        var reports = await _reportSvc.GetReportsAsync(_currentUser.OrganizationId, ct);
        return Ok(reports);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto, CancellationToken ct)
    {
        var report = await _reportSvc.CreateReportAsync(dto, _currentUser.OrganizationId, ct);
        return Ok(report);
    }

    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody] ExportRequestDto dto, CancellationToken ct)
    {
        var bytes = await _reportSvc.ExportAsync(dto, _currentUser.OrganizationId, ct);

        var contentType = dto.Format switch
        {
            ReportFormat.Csv => "text/csv",
            ReportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ReportFormat.Pdf => "application/pdf",
            _ => "text/csv"
        };

        var extension = dto.Format switch
        {
            ReportFormat.Csv => "csv",
            ReportFormat.Excel => "xlsx",
            ReportFormat.Pdf => "pdf",
            _ => "csv"
        };

        return File(bytes, contentType, $"report-{DateTime.UtcNow:yyyyMMdd}.{extension}");
    }

    [HttpGet("scheduled")]
    public async Task<IActionResult> GetScheduledReports(CancellationToken ct)
    {
        var reports = await _reportSvc.GetScheduledReportsAsync(_currentUser.OrganizationId, ct);
        return Ok(reports);
    }

    [HttpPost("scheduled")]
    public async Task<IActionResult> SaveScheduledReport([FromBody] SaveScheduledReportDto dto, CancellationToken ct)
    {
        var report = await _reportSvc.SaveScheduledReportAsync(dto, _currentUser.OrganizationId, ct);
        return Ok(report);
    }

    [HttpDelete("scheduled/{id:guid}")]
    public async Task<IActionResult> DeleteScheduledReport(Guid id, CancellationToken ct)
    {
        await _reportSvc.DeleteScheduledReportAsync(id, ct);
        return NoContent();
    }
}
