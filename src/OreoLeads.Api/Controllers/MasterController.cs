using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Api.Controllers;

/// <summary>
/// Master admin panel — protected by X-Master-Password header checked against
/// the MASTER_PANEL_PASSWORD environment variable. Never exposed in Swagger.
/// </summary>
[ApiController]
[Route("api/master")]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public class MasterController : ControllerBase
{
    private readonly IInviteCodeService _inviteCodes;
    private readonly string             _masterPassword;

    public MasterController(IInviteCodeService inviteCodes, IConfiguration configuration)
    {
        _inviteCodes    = inviteCodes;
        _masterPassword = configuration["MASTER_PANEL_PASSWORD"] ?? string.Empty;
    }

    // ── Auth check ────────────────────────────────────────────────────────────

    [HttpPost("verify")]
    public IActionResult Verify([FromHeader(Name = "X-Master-Password")] string? password)
        => IsAuthorized(password) ? Ok(new { ok = true }) : Unauthorized(new { message = "Mot de passe incorrect." });

    // ── Invite codes ──────────────────────────────────────────────────────────

    [HttpGet("invite-codes")]
    public async Task<IActionResult> List(
        [FromHeader(Name = "X-Master-Password")] string? password,
        CancellationToken ct)
    {
        if (!IsAuthorized(password)) return Unauthorized();

        var codes = await _inviteCodes.GetAllAsync(ct);
        return Ok(codes.Select(c => new
        {
            id        = c.Id,
            code      = c.Code,
            note      = c.Note,
            isUsed    = c.IsUsed,
            usedBy    = c.UsedByEmail,
            usedAt    = c.UsedAt,
            expiresAt = c.ExpiresAt,
            createdAt = c.CreatedAt,
        }));
    }

    [HttpPost("invite-codes/generate")]
    public async Task<IActionResult> Generate(
        [FromHeader(Name = "X-Master-Password")] string? password,
        [FromBody] GenerateInviteCodesRequest body,
        CancellationToken ct)
    {
        if (!IsAuthorized(password)) return Unauthorized();

        var count = Math.Clamp(body.Count, 1, 50);
        DateTime? expiresAt = body.ExpiresInDays.HasValue
            ? DateTime.UtcNow.AddDays(body.ExpiresInDays.Value)
            : null;

        var codes = await _inviteCodes.GenerateAsync(count, body.Note, expiresAt, ct);
        return Ok(codes.Select(c => new { c.Id, c.Code, c.Note, c.ExpiresAt }));
    }

    [HttpDelete("invite-codes/{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromHeader(Name = "X-Master-Password")] string? password,
        Guid id,
        CancellationToken ct)
    {
        if (!IsAuthorized(password)) return Unauthorized();

        var deleted = await _inviteCodes.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool IsAuthorized(string? password)
    {
        if (string.IsNullOrWhiteSpace(_masterPassword)) return false;
        return password == _masterPassword;
    }
}

public record GenerateInviteCodesRequest(int Count, string? Note, int? ExpiresInDays);
