using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Analytics.DTOs;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsDashboardController : ControllerBase
{
    private readonly IAnalyticsService _analyticsSvc;
    private readonly ICurrentUserService _currentUser;

    public AnalyticsDashboardController(IAnalyticsService analyticsSvc, ICurrentUserService currentUser)
    {
        _analyticsSvc = analyticsSvc;
        _currentUser = currentUser;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetExecutiveDashboard(
        [FromQuery] DateRangePreset preset = DateRangePreset.Last30Days,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = new DateRangeDto(preset, from, to);
        var result = await _analyticsSvc.GetExecutiveDashboardAsync(_currentUser.OrganizationId, range, ct);
        return Ok(result);
    }

    [HttpGet("kpi")]
    public async Task<IActionResult> GetKpiSummary(
        [FromQuery] DateRangePreset preset = DateRangePreset.Last30Days,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = new DateRangeDto(preset, from, to);
        var result = await _analyticsSvc.GetKpiSummaryAsync(_currentUser.OrganizationId, range, ct);
        return Ok(result);
    }

    [HttpGet("leads/series")]
    public async Task<IActionResult> GetLeadTimeSeries(
        [FromQuery] DateRangePreset preset = DateRangePreset.Last30Days,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = new DateRangeDto(preset, from, to);
        var result = await _analyticsSvc.GetLeadTimeSeriesAsync(_currentUser.OrganizationId, range, ct);
        return Ok(result);
    }

    [HttpGet("emails/series")]
    public async Task<IActionResult> GetEmailTimeSeries(
        [FromQuery] DateRangePreset preset = DateRangePreset.Last30Days,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = new DateRangeDto(preset, from, to);
        var result = await _analyticsSvc.GetEmailTimeSeriesAsync(_currentUser.OrganizationId, range, ct);
        return Ok(result);
    }

    [HttpGet("funnel")]
    public async Task<IActionResult> GetSalesFunnel(
        [FromQuery] DateRangePreset preset = DateRangePreset.Last30Days,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = new DateRangeDto(preset, from, to);
        var result = await _analyticsSvc.GetSalesFunnelAsync(_currentUser.OrganizationId, range, ct);
        return Ok(result);
    }

    [HttpGet("emails")]
    public async Task<IActionResult> GetEmailAnalytics(
        [FromQuery] DateRangePreset preset = DateRangePreset.Last30Days,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = new DateRangeDto(preset, from, to);
        var result = await _analyticsSvc.GetEmailAnalyticsAsync(_currentUser.OrganizationId, range, ct);
        return Ok(result);
    }

    [HttpGet("automation")]
    public async Task<IActionResult> GetAutomationAnalytics(
        [FromQuery] DateRangePreset preset = DateRangePreset.Last30Days,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = new DateRangeDto(preset, from, to);
        var result = await _analyticsSvc.GetAutomationAnalyticsAsync(_currentUser.OrganizationId, range, ct);
        return Ok(result);
    }

    [HttpGet("airtable")]
    public async Task<IActionResult> GetAirtableAnalytics(
        [FromQuery] DateRangePreset preset = DateRangePreset.Last30Days,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var range = new DateRangeDto(preset, from, to);
        var result = await _analyticsSvc.GetAirtableAnalyticsAsync(_currentUser.OrganizationId, range, ct);
        return Ok(result);
    }

    [HttpGet("monitoring")]
    public async Task<IActionResult> GetSystemMonitoring(CancellationToken ct)
    {
        var result = await _analyticsSvc.GetSystemMonitoringAsync(_currentUser.OrganizationId, ct);
        return Ok(result);
    }
}
