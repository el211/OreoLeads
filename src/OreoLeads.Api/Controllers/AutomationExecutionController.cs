using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/automation/executions")]
[Authorize]
public class AutomationExecutionController : ControllerBase
{
    private readonly IAutomationWorkflowService _workflowSvc;
    private readonly IAutomationEngine _engine;

    public AutomationExecutionController(
        IAutomationWorkflowService workflowSvc,
        IAutomationEngine engine)
    {
        _workflowSvc = workflowSvc;
        _engine = engine;
    }

    [HttpGet]
    public async Task<IActionResult> GetExecutions(
        [FromQuery] Guid? workflowId, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var executions = await _workflowSvc.GetExecutionsAsync(workflowId, orgId, ct);
        return Ok(executions);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetExecution(Guid id, CancellationToken ct)
    {
        var execution = await _workflowSvc.GetExecutionAsync(id, ct);
        if (execution is null) return NotFound();
        return Ok(execution);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelExecution(Guid id, CancellationToken ct)
    {
        await _engine.CancelExecutionAsync(id, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> RetryExecution(Guid id, CancellationToken ct)
    {
        await _engine.RetryExecutionAsync(id, ct);
        return Ok();
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<IActionResult> GetExecutionLogs(Guid id, CancellationToken ct)
    {
        var logs = await _workflowSvc.GetExecutionLogsAsync(id, ct);
        return Ok(logs);
    }

    private Guid? GetOrganizationId()
    {
        var claim = User.FindFirst("organizationId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
