using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Auth.DTOs;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    /// <summary>Register a new user (creates a new Organization if organizationName is provided).</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto, GetIpAddress(), ct);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>Authenticate and obtain JWT tokens.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _authService.LoginAsync(dto, GetIpAddress(), ct);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>Exchange a valid refresh token for a new token pair.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Refresh(CancellationToken ct)
    {
        var token = Request.Cookies["refreshToken"]
            ?? Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { Message = "Refresh token is required." });

        try
        {
            var result = await _authService.RefreshTokenAsync(token, GetIpAddress(), ct);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>Revoke refresh token and clear the auth cookie.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _authService.LogoutAsync(_currentUser.UserId!, ct);
        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    /// <summary>Return info about the currently authenticated user.</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            UserId = _currentUser.UserId,
            Email = _currentUser.UserEmail,
            Name = _currentUser.UserName,
            OrganizationId = _currentUser.OrganizationId,
        });
    }

    /// <summary>Change the current user's password.</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        try
        {
            await _authService.ChangePasswordAsync(_currentUser.UserId!, dto, ct);
            Response.Cookies.Delete("refreshToken");
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>Request a password-reset email.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordDto dto, CancellationToken ct)
    {
        await _authService.ForgotPasswordAsync(dto, ct);
        return Ok(new { Message = "If the email exists, a reset link has been sent." });
    }

    /// <summary>Reset password using the token received by email.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        try
        {
            await _authService.ResetPasswordAsync(dto, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GetIpAddress()
        => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append("refreshToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
        });
    }
}
