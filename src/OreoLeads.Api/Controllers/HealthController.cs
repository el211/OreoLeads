using Microsoft.AspNetCore.Mvc;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "OreoLeads API",
            Timestamp = DateTime.UtcNow
        });
    }
}
