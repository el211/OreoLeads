using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/analytics/forecast")]
[Authorize]
public class AnalyticsForecastController : ControllerBase
{
    private readonly IForecastService _forecastSvc;
    private readonly ICurrentUserService _currentUser;

    public AnalyticsForecastController(IForecastService forecastSvc, ICurrentUserService currentUser)
    {
        _forecastSvc = forecastSvc;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetForecastSummary(CancellationToken ct)
    {
        var result = await _forecastSvc.GetForecastSummaryAsync(_currentUser.OrganizationId, ct);
        return Ok(result);
    }

    [HttpGet("leads")]
    public async Task<IActionResult> ForecastLeads([FromQuery] int daysAhead = 30, CancellationToken ct = default)
    {
        var result = await _forecastSvc.ForecastLeadsAsync(_currentUser.OrganizationId, daysAhead, ct);
        return Ok(result);
    }

    [HttpGet("conversions")]
    public async Task<IActionResult> ForecastConversions([FromQuery] int daysAhead = 30, CancellationToken ct = default)
    {
        var result = await _forecastSvc.ForecastConversionsAsync(_currentUser.OrganizationId, daysAhead, ct);
        return Ok(result);
    }

    [HttpGet("emails")]
    public async Task<IActionResult> ForecastEmails([FromQuery] int daysAhead = 30, CancellationToken ct = default)
    {
        var result = await _forecastSvc.ForecastEmailsAsync(_currentUser.OrganizationId, daysAhead, ct);
        return Ok(result);
    }
}
