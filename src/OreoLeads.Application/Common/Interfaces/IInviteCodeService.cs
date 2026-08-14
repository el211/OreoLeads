using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Common.Interfaces;

public interface IInviteCodeService
{
    /// <summary>Generates <paramref name="count"/> unique invite codes and persists them.</summary>
    Task<IReadOnlyList<InviteCode>> GenerateAsync(int count, string? note, DateTime? expiresAt, CancellationToken ct = default);

    /// <summary>Returns true and marks the code as used if it is valid and unused.</summary>
    Task<bool> ValidateAndConsumeAsync(string code, string usedByEmail, CancellationToken ct = default);

    Task<IReadOnlyList<InviteCode>> GetAllAsync(CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
