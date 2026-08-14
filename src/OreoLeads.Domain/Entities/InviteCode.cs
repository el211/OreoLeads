using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

public class InviteCode : BaseEntity
{
    public string Code        { get; set; } = string.Empty;
    public string? Note       { get; set; }
    public bool   IsUsed      { get; set; }
    public string? UsedByEmail { get; set; }
    public DateTime? UsedAt   { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
