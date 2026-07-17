using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/automation/workflows")]
[Authorize]
public class AutomationWorkflowController : ControllerBase
{
    private readonly IAutomationWorkflowService _workflowSvc;
    private readonly IAutomationEngine _engine;

    public AutomationWorkflowController(
        IAutomationWorkflowService workflowSvc,
        IAutomationEngine engine)
    {
        _workflowSvc = workflowSvc;
        _engine = engine;
    }

    [HttpGet]
    public async Task<IActionResult> GetWorkflows(CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var workflows = await _workflowSvc.GetWorkflowsAsync(orgId, ct);
        return Ok(workflows);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkflow(
        [FromBody] CreateAutomationWorkflowDto dto, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var workflow = await _workflowSvc.CreateWorkflowAsync(dto, orgId, ct);
        return Ok(workflow);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetWorkflow(Guid id, CancellationToken ct)
    {
        var workflow = await _workflowSvc.GetWorkflowAsync(id, ct);
        if (workflow is null) return NotFound();
        return Ok(workflow);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateWorkflow(
        Guid id, [FromBody] UpdateAutomationWorkflowDto dto, CancellationToken ct)
    {
        var workflow = await _workflowSvc.UpdateWorkflowAsync(id, dto, ct);
        return Ok(workflow);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteWorkflow(Guid id, CancellationToken ct)
    {
        await _workflowSvc.DeleteWorkflowAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/execute")]
    public async Task<IActionResult> ExecuteWorkflow(Guid id, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var triggerEvent = new TriggerEventDto(
            Domain.Enums.TriggerType.Manual, null, orgId,
            new Dictionary<string, object?>());
        var result = await _engine.ExecuteWorkflowAsync(id, triggerEvent, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/clone")]
    public async Task<IActionResult> CloneWorkflow(Guid id, CancellationToken ct)
    {
        var clone = await _workflowSvc.CloneWorkflowAsync(id, ct);
        return Ok(clone);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateWorkflow(Guid id, CancellationToken ct)
    {
        await _workflowSvc.ActivateAsync(id, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateWorkflow(Guid id, CancellationToken ct)
    {
        await _workflowSvc.DeactivateAsync(id, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/pause")]
    public async Task<IActionResult> PauseWorkflow(Guid id, CancellationToken ct)
    {
        await _workflowSvc.PauseAsync(id, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<IActionResult> ResumeWorkflow(Guid id, CancellationToken ct)
    {
        await _workflowSvc.ResumeAsync(id, ct);
        return Ok();
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> GetVersions(Guid id, CancellationToken ct)
    {
        var versions = await _workflowSvc.GetVersionsAsync(id, ct);
        return Ok(versions);
    }

    [HttpGet("export/{id:guid}")]
    public async Task<IActionResult> ExportWorkflow(Guid id, CancellationToken ct)
    {
        var json = await _workflowSvc.ExportWorkflowAsync(id, ct);
        return Content(json, "application/json");
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportWorkflow(
        [FromBody] ImportWorkflowRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var workflow = await _workflowSvc.ImportWorkflowAsync(request.Json, orgId, ct);
        return Ok(workflow);
    }

    private Guid? GetOrganizationId()
    {
        var claim = User.FindFirst("organizationId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public record ImportWorkflowRequest(string Json);
