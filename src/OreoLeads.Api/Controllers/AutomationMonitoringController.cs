using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/automation/monitoring")]
[Authorize]
public class AutomationMonitoringController : ControllerBase
{
    private readonly IAutomationWorkflowService _workflowSvc;
    private readonly IAutomationQueue _queue;

    public AutomationMonitoringController(
        IAutomationWorkflowService workflowSvc,
        IAutomationQueue queue)
    {
        _workflowSvc = workflowSvc;
        _queue = queue;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var stats = await _workflowSvc.GetMonitoringStatsAsync(orgId, ct);
        return Ok(stats);
    }

    [HttpGet("queue")]
    public async Task<IActionResult> GetQueueItems(CancellationToken ct)
    {
        var items = await _queue.GetPendingItemsAsync(100, ct);
        return Ok(items);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveJobs(CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var executions = await _workflowSvc.GetExecutionsAsync(null, orgId, ct);
        var active = executions.Where(e =>
            e.Status == Domain.Enums.ExecutionStatus.Running ||
            e.Status == Domain.Enums.ExecutionStatus.Pending).ToList();
        return Ok(active);
    }

    private Guid? GetOrganizationId()
    {
        var claim = User.FindFirst("organizationId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
