using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Auth.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Identity;

internal sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtService _jwtService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtService jwtService,
        ApplicationDbContext context,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _context = context;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterDto dto, string ipAddress, CancellationToken ct = default)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) != null)
            throw new InvalidOperationException($"Email '{dto.Email}' is already taken.");

        Organization? org = null;
        if (!string.IsNullOrWhiteSpace(dto.OrganizationName))
        {
            org = new Organization
            {
                Name = dto.OrganizationName,
                Slug = GenerateSlug(dto.OrganizationName),
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync(ct);
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            OrganizationId = org?.Id,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Registration failed: {errors}");
        }

        await _userManager.AddToRoleAsync(user, org != null ? "Admin" : "Sales");

        return await BuildAuthResponseAsync(user, ipAddress, ct);
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginDto dto, string ipAddress, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                throw new UnauthorizedAccessException("Account is temporarily locked. Try again later.");
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return await BuildAuthResponseAsync(user, ipAddress, ct);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(
        string refreshToken, string ipAddress, CancellationToken ct = default)
    {
        var token = await _context.RefreshTokens
            .Include(r => r.UserId)
            .FirstOrDefaultAsync(r => r.Token == refreshToken, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!token.IsActive)
            throw new UnauthorizedAccessException("Refresh token is no longer valid.");

        var user = await _userManager.FindByIdAsync(token.UserId)
            ?? throw new UnauthorizedAccessException("User not found.");

        // Rotate token
        token.RevokedAt = DateTime.UtcNow;

        return await BuildAuthResponseAsync(user, ipAddress, ct, revokeOldToken: token);
    }

    public async Task RevokeTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken, ct)
            ?? throw new KeyNotFoundException("Refresh token not found.");

        token.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task LogoutAsync(string userId, CancellationToken ct = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(
        string userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Password change failed: {errors}");
        }

        // Revoke all refresh tokens on password change
        await LogoutAsync(userId, ct);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) return; // Silent — do not reveal user existence

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        _logger.LogInformation("Password reset token generated for {Email}: {Token}", dto.Email, token);
        // TODO: send email via Brevo (next step)
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email)
            ?? throw new KeyNotFoundException("User not found.");

        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Password reset failed: {errors}");
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<AuthResponseDto> BuildAuthResponseAsync(
        ApplicationUser user,
        string ipAddress,
        CancellationToken ct,
        RefreshToken? revokeOldToken = null)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var org = user.OrganizationId.HasValue
            ? await _context.Organizations.FindAsync([user.OrganizationId.Value], ct)
            : null;

        var (accessToken, expiresAt) = _jwtService.GenerateAccessToken(
            user.Id, user.Email!, user.FirstName, user.LastName, user.OrganizationId, roles);

        var (refreshTokenValue, refreshExpiresAt) = _jwtService.GenerateRefreshToken();

        if (revokeOldToken != null)
            revokeOldToken.ReplacedByToken = refreshTokenValue;

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = refreshExpiresAt,
            IpAddress = ipAddress,
        };
        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(ct);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            ExpiresAt: expiresAt,
            User: new UserDto(
                Id: user.Id,
                Email: user.Email!,
                FirstName: user.FirstName,
                LastName: user.LastName,
                FullName: user.FullName,
                OrganizationId: user.OrganizationId,
                OrganizationName: org?.Name,
                Roles: roles));
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace("'", "")
            .Replace("\"", "");
        return slug[..Math.Min(slug.Length, 100)];
    }
}
