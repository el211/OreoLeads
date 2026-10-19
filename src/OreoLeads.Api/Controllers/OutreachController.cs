using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Prospecting;
using OreoLeads.Infrastructure.Brevo;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/outreach")]
public class OutreachController(ApplicationDbContext db, ICurrentUserService user, OutreachReviewService service) : ControllerBase
{
    [Authorize, HttpGet("reviews/{draftId:guid}")]
    public async Task<IActionResult> Review(Guid draftId, CancellationToken ct)
    {
        if (user.OrganizationId is not { } org) return Forbid();
        var draft = await db.GeneratedEmails.SingleOrDefaultAsync(e => e.Id == draftId && e.OrganizationId == org, ct);
        if (draft == null) return NotFound();
        var review = await db.OutreachReviews.SingleOrDefaultAsync(r => r.EmailDraftId == draftId && r.OrganizationId == org, ct);
        return Ok(new { required = draft.RequiresOutreachReview, review });
    }

    [Authorize, HttpPost("reviews/{draftId:guid}")]
    public async Task<IActionResult> Prepare(Guid draftId, OutreachReviewRequest request, CancellationToken ct)
    {
        if (user.OrganizationId is not { } org || user.UserId is not { } userId) return Forbid();
        var draft = await db.GeneratedEmails.SingleOrDefaultAsync(e => e.Id == draftId && e.OrganizationId == org, ct);
        if (draft == null) return NotFound();
        try { return Ok(await service.PrepareAsync(draft, request, org, userId, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [AllowAnonymous, HttpGet("unsubscribe")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public IActionResult Confirm([FromQuery] string token)
    {
        if (service.ReadToken(token) == null) return BadRequest("Invalid unsubscribe link.");
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        return Content($"<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\"><title>Unsubscribe</title><body><h1>Stop business outreach / Arrêter la prospection</h1><form method=\"post\" action=\"/api/outreach/unsubscribe?token={WebUtility.HtmlEncode(token)}\"><button type=\"submit\">Unsubscribe / Me désinscrire</button></form></body></html>", "text/html");
    }

    [AllowAnonymous, HttpPost("unsubscribe")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> Unsubscribe([FromQuery] string token, CancellationToken ct)
    {
        var payload = service.ReadToken(token);
        if (payload == null) return BadRequest("Invalid unsubscribe link.");
        await service.UnsubscribeAsync(payload, ct);
        Response.Headers.CacheControl = "no-store";
        return Content("You will no longer receive business outreach. Votre désinscription est enregistrée.", "text/plain");
    }
}
