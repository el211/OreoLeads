using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Analytics.DTOs;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsWidgetController : ControllerBase
{
    private readonly IWidgetService _widgetSvc;
    private readonly ICurrentUserService _currentUser;

    public AnalyticsWidgetController(IWidgetService widgetSvc, ICurrentUserService currentUser)
    {
        _widgetSvc = widgetSvc;
        _currentUser = currentUser;
    }

    [HttpGet("dashboards")]
    public async Task<IActionResult> GetDashboards(CancellationToken ct)
    {
        var dashboards = await _widgetSvc.GetDashboardsAsync(_currentUser.OrganizationId, _currentUser.UserId, ct);
        return Ok(dashboards);
    }

    [HttpPost("dashboards")]
    public async Task<IActionResult> SaveDashboard([FromBody] SaveDashboardDto dto, CancellationToken ct)
    {
        var dashboard = await _widgetSvc.SaveDashboardAsync(dto, _currentUser.OrganizationId, _currentUser.UserId, ct);
        return Ok(dashboard);
    }

    [HttpGet("dashboards/{id:guid}/widgets")]
    public async Task<IActionResult> GetWidgets(Guid id, CancellationToken ct)
    {
        var widgets = await _widgetSvc.GetWidgetsAsync(id, ct);
        return Ok(widgets);
    }

    [HttpPost("dashboards/{id:guid}/widgets")]
    public async Task<IActionResult> AddWidget(Guid id, [FromBody] AddWidgetDto dto, CancellationToken ct)
    {
        var widget = await _widgetSvc.AddWidgetAsync(dto, _currentUser.OrganizationId, ct);
        return Ok(widget);
    }

    [HttpPut("widgets/{id:guid}")]
    public async Task<IActionResult> UpdateWidget(Guid id, [FromBody] UpdateWidgetDto dto, CancellationToken ct)
    {
        var widget = await _widgetSvc.UpdateWidgetAsync(id, dto, ct);
        return Ok(widget);
    }

    [HttpDelete("widgets/{id:guid}")]
    public async Task<IActionResult> DeleteWidget(Guid id, CancellationToken ct)
    {
        await _widgetSvc.DeleteWidgetAsync(id, ct);
        return NoContent();
    }

    [HttpPut("dashboards/{id:guid}/layout")]
    public async Task<IActionResult> SaveLayout(Guid id, [FromBody] LayoutUpdateRequest request, CancellationToken ct)
    {
        await _widgetSvc.SaveLayoutAsync(id, request.LayoutJson, ct);
        return Ok();
    }
}

public record LayoutUpdateRequest(string LayoutJson);
