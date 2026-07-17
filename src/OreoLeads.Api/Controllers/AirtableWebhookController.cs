using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Airtable.DTOs;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Api.Controllers;

/// <summary>
/// Receives Airtable webhook ping notifications.
///
/// IMPORTANT: Airtable webhooks use a ping-then-poll model. When a change occurs:
/// 1. Airtable sends a lightweight ping to this endpoint (no event data included).
/// 2. This endpoint identifies the relevant config and calls PollChangesAsync,
///    which then calls GetWebhookChangesAsync to retrieve the actual change data.
///
/// This is different from Brevo webhooks which include full event data in the ping.
/// No authentication is required on this endpoint (Airtable calls it externally),
/// but we validate the baseId and webhookId against known configurations.
/// </summary>
[ApiController]
[Route("api/webhooks/airtable")]
public class AirtableWebhookController : ControllerBase
{
    private readonly ApplicationDbContext   _db;
    private readonly IAirtableWebhookService _webhookSvc;

    public AirtableWebhookController(
        ApplicationDbContext    db,
        IAirtableWebhookService webhookSvc)
    {
        _db         = db;
        _webhookSvc = webhookSvc;
    }

    /// <summary>
    /// Receives a ping from Airtable indicating that changes have occurred.
    /// After validating the payload, enqueues a poll to fetch the actual changes.
    /// </summary>
    [HttpPost("ping")]
    public async Task<IActionResult> HandlePing(CancellationToken ct)
    {
        AirtableWebhookPayloadDto? payload;
        try
        {
            using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
            payload = JsonSerializer.Deserialize<AirtableWebhookPayloadDto>(
                doc.RootElement.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return BadRequest("Invalid JSON payload.");
        }

        if (payload is null) return BadRequest("Empty payload.");

        // Find the matching configuration by baseId and webhookId
        var config = await _db.AirtableConfigurations
                              .FirstOrDefaultAsync(c =>
                                  c.BaseId == payload.BaseId &&
                                  c.WebhookId == payload.WebhookId, ct);

        if (config is null)
        {
            // Unknown webhook — return 200 to prevent Airtable from retrying
            return Ok();
        }

        // Trigger an async poll for the changes
        _ = Task.Run(async () =>
        {
            try
            {
                await _webhookSvc.PollChangesAsync(config.Id, CancellationToken.None);
            }
            catch
            {
                // Swallow — errors are logged inside PollChangesAsync
            }
        }, CancellationToken.None);

        // Airtable expects a 200 response quickly
        return Ok();
    }
}
