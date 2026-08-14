namespace OreoLeads.Application.Common.Interfaces;

public interface IMasterAdminService
{
    Task<MasterStatsDto>          GetStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MasterUserDto>> GetUsersAsync(CancellationToken ct = default);
    Task<bool>   BanUserAsync(string userId, CancellationToken ct = default);
    Task<bool>   UnbanUserAsync(string userId, CancellationToken ct = default);
    /// <summary>Locks the account — user sees "compte verrouillé" on next login attempt.</summary>
    Task<bool>   LockUserAsync(string userId, CancellationToken ct = default);
    Task<bool>   UnlockUserAsync(string userId, CancellationToken ct = default);
    Task<bool>   DeleteUserAsync(string userId, CancellationToken ct = default);
    /// <summary>Resets the user's password to a random value and returns it.</summary>
    Task<string?> ResetPasswordAsync(string userId, CancellationToken ct = default);
    /// <summary>Returns a short-lived JWT access token for the given user (impersonation).</summary>
    Task<string?> GenerateImpersonationTokenAsync(string userId, CancellationToken ct = default);
}

public record MasterStatsDto(int TotalUsers, int ActiveUsers, int BannedUsers, int TotalLeads, int TotalInviteCodes);

public record MasterUserDto(
    string   Id,
    string   Email,
    string   FirstName,
    string   LastName,
    bool     IsActive,
    bool     IsLocked,
    string?  OrganizationName,
    string[] Roles,
    DateTime CreatedAt);
