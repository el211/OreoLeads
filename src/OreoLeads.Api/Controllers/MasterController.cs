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
    private readonly IInviteCodeService  _inviteCodes;
    private readonly IMasterAdminService _admin;
    private readonly string              _masterPassword;

    public MasterController(IInviteCodeService inviteCodes, IMasterAdminService admin, IConfiguration configuration)
    {
        _inviteCodes    = inviteCodes;
        _admin          = admin;
        _masterPassword = Environment.GetEnvironmentVariable("MASTER_PANEL_PASSWORD")
                          ?? configuration["MASTER_PANEL_PASSWORD"]
                          ?? string.Empty;
    }

    // ── Auth check ────────────────────────────────────────────────────────────

    [HttpPost("verify")]
    public IActionResult Verify([FromHeader(Name = "X-Master-Password")] string? password)
        => IsAuthorized(password) ? Ok(new { ok = true }) : Unauthorized(new { message = "Mot de passe incorrect." });

    // ── Stats ─────────────────────────────────────────────────────────────────

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromHeader(Name = "X-Master-Password")] string? password,
        CancellationToken ct)
    {
        if (!IsAuthorized(password)) return Unauthorized();
        var stats = await _admin.GetStatsAsync(ct);
        return Ok(stats);
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(
        [FromHeader(Name = "X-Master-Password")] string? password,
        CancellationToken ct)
    {
        if (!IsAuthorized(password)) return Unauthorized();
        var users = await _admin.GetUsersAsync(ct);
        return Ok(users);
    }

    [HttpPost("users/{id}/ban")]
    public async Task<IActionResult> BanUser(
        [FromHeader(Name = "X-Master-Password")] string? password,
        string id, CancellationToken ct)
    {
        if (!IsAuthorized(password)) return Unauthorized();
        return await _admin.BanUserAsync(id, ct) ? Ok() : NotFound();
    }

    [HttpPost("users/{id}/unban")]
    public async Task<IActionResult> UnbanUser(
        [FromHeader(Name = "X-Master-Password")] string? password,
        string id, CancellationToken ct)
    {
        if (!IsAuthorized(password)) return Unauthorized();
        return await _admin.UnbanUserAsync(id, ct) ? Ok() : NotFound();
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(
        [FromHeader(Name = "X-Master-Password")] string? password,
        string id, CancellationToken ct)
    {
        if (!IsAuthorized(password)) return Unauthorized();
        return await _admin.DeleteUserAsync(id, ct) ? NoContent() : NotFound();
    }

    [HttpPost("users/{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromHeader(Name = "X-Master-Password")] string? password,
        string id, CancellationToken ct)
    {
        if (!IsAuthorized(password)) return Unauthorized();
        var newPw = await _admin.ResetPasswordAsync(id, ct);
        return newPw != null ? Ok(new { newPassword = newPw }) : NotFound();
    }

    [HttpPost("users/{id}/impersonate")]
    public async Task<IActionResult> Impersonate(
        [FromHeader(Name = "X-Master-Password")] string? password,
        string id, CancellationToken ct)
    {
        if (!IsAuthorized(password)) return Unauthorized();
        var token = await _admin.GenerateImpersonationTokenAsync(id, ct);
        return token != null ? Ok(new { token }) : NotFound();
    }

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
