using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Identity;

internal sealed class InviteCodeService : IInviteCodeService
{
    private readonly ApplicationDbContext _db;

    public InviteCodeService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<InviteCode>> GenerateAsync(
        int count, string? note, DateTime? expiresAt, CancellationToken ct = default)
    {
        var codes = new List<InviteCode>(count);
        for (var i = 0; i < count; i++)
        {
            codes.Add(new InviteCode
            {
                Code      = GenerateCode(),
                Note      = note,
                ExpiresAt = expiresAt,
            });
        }

        _db.InviteCodes.AddRange(codes);
        await _db.SaveChangesAsync(ct);
        return codes;
    }

    public async Task<bool> ValidateAndConsumeAsync(
        string code, string usedByEmail, CancellationToken ct = default)
    {
        var invite = await _db.InviteCodes
            .FirstOrDefaultAsync(c => c.Code == code.Trim().ToUpperInvariant(), ct);

        if (invite is null || invite.IsUsed)
            return false;

        if (invite.ExpiresAt.HasValue && invite.ExpiresAt.Value < DateTime.UtcNow)
            return false;

        invite.IsUsed      = true;
        invite.UsedByEmail = usedByEmail;
        invite.UsedAt      = DateTime.UtcNow;
        invite.SetUpdatedAt();

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<InviteCode>> GetAllAsync(CancellationToken ct = default)
        => await _db.InviteCodes.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var invite = await _db.InviteCodes.FindAsync([id], ct);
        if (invite is null || invite.IsUsed) return false;

        _db.InviteCodes.Remove(invite);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Generates a human-readable 10-char code like "A3BX-K9QW".</summary>
    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no I/O/0/1 to avoid confusion
        var part1 = new string(Enumerable.Range(0, 4).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
        var part2 = new string(Enumerable.Range(0, 4).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
        return $"{part1}-{part2}";
    }
}
