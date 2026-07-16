using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>Statistiques globales du dashboard</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var stats = await _dashboardService.GetStatsAsync(ct);
        return Ok(stats);
    }
}
