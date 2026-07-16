using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/leads/{leadId:guid}/activities")]
public class LeadActivitiesController : ControllerBase
{
    private readonly ILeadActivityRepository _repository;
    private readonly ILeadRepository _leadRepository;

    public LeadActivitiesController(
        ILeadActivityRepository repository,
        ILeadRepository leadRepository)
    {
        _repository = repository;
        _leadRepository = leadRepository;
    }

    /// <summary>Obtenir toutes les activités d'un prospect</summary>
    [HttpGet]
    public async Task<IActionResult> GetByLead(Guid leadId, CancellationToken ct)
    {
        if (!await _leadRepository.ExistsAsync(leadId, ct)) return NotFound();
        var activities = await _repository.GetByLeadIdAsync(leadId, ct);
        return Ok(activities);
    }
}
