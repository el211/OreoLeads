using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/automation/templates")]
[Authorize]
public class AutomationTemplateController : ControllerBase
{
    private readonly IAutomationWorkflowService _workflowSvc;

    public AutomationTemplateController(IAutomationWorkflowService workflowSvc)
        => _workflowSvc = workflowSvc;

    [HttpGet]
    public async Task<IActionResult> GetTemplates(CancellationToken ct)
    {
        var templates = await _workflowSvc.GetTemplatesAsync(ct);
        return Ok(templates);
    }

    [HttpPost("{id:guid}/use")]
    public async Task<IActionResult> UseTemplate(Guid id, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var workflow = await _workflowSvc.UseTemplateAsync(id, orgId, ct);
        return Ok(workflow);
    }

    private Guid? GetOrganizationId()
    {
        var claim = User.FindFirst("organizationId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
