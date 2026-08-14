using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Identity;

internal class MasterAdminService : IMasterAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext         _db;
    private readonly JwtService                   _jwt;

    public MasterAdminService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        JwtService jwt)
    {
        _userManager = userManager;
        _db          = db;
        _jwt         = jwt;
    }

    public async Task<MasterStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var users       = await _userManager.Users.ToListAsync(ct);
        var totalLeads  = await _db.Leads.IgnoreQueryFilters().CountAsync(ct);
        var totalCodes  = await _db.InviteCodes.CountAsync(ct);

        return new MasterStatsDto(
            TotalUsers:      users.Count,
            ActiveUsers:     users.Count(u => u.IsActive),
            BannedUsers:     users.Count(u => !u.IsActive),
            TotalLeads:      totalLeads,
            TotalInviteCodes: totalCodes);
    }

    public async Task<IReadOnlyList<MasterUserDto>> GetUsersAsync(CancellationToken ct = default)
    {
        var users = await _userManager.Users
            .Include(u => u.Organization)
            .OrderBy(u => u.Email)
            .ToListAsync(ct);

        var result = new List<MasterUserDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var isLocked = u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow;
            result.Add(new MasterUserDto(
                Id:               u.Id,
                Email:            u.Email ?? string.Empty,
                FirstName:        u.FirstName,
                LastName:         u.LastName,
                IsActive:         u.IsActive,
                IsLocked:         isLocked,
                OrganizationName: u.Organization?.Name,
                Roles:            roles.ToArray(),
                CreatedAt:        DateTime.UtcNow));
        }
        return result;
    }

    public async Task<bool> BanUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        user.IsActive = false;
        // Revoke all refresh tokens
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
        await _userManager.UpdateAsync(user);
        return true;
    }

    public async Task<bool> UnbanUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        user.IsActive = true;
        await _userManager.UpdateAsync(user);
        return true;
    }

    public async Task<bool> LockUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        return true;
    }

    public async Task<bool> UnlockUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        return true;
    }

    public async Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        await _userManager.DeleteAsync(user);
        return true;
    }

    public async Task<string?> ResetPasswordAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var newPassword = GeneratePassword();
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded ? newPassword : null;
    }

    public async Task<string?> GenerateImpersonationTokenAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var (token, _) = _jwt.GenerateAccessToken(
            userId:         user.Id,
            email:          user.Email ?? string.Empty,
            firstName:      user.FirstName,
            lastName:       user.LastName,
            organizationId: user.OrganizationId,
            roles:          roles);

        return token;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string GeneratePassword()
    {
        const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower   = "abcdefghjklmnpqrstuvwxyz";
        const string digits  = "23456789";
        const string special = "!@#$%";
        const string all     = upper + lower + digits + special;

        var chars = new char[12];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];
        for (int i = 4; i < 12; i++)
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(100)).ToArray());
    }
}
