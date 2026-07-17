using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

/// <summary>Single-row table: stores the active Brevo (Sendinblue) email configuration.</summary>
public class BrevoConfiguration : BaseEntity
{
    public Guid?   OrganizationId   { get; set; }
    public string? EncryptedApiKey  { get; set; }
    public string  SenderName       { get; set; } = string.Empty;
    public string  SenderEmail      { get; set; } = string.Empty;
    public string? ReplyTo          { get; set; }
    public bool    IsEnabled        { get; set; } = false;
    public bool    TestMode         { get; set; } = false;
    public string? TestModeEmail    { get; set; }
    public int     DailyLimit       { get; set; } = 300;
    public string? WebhookSecret    { get; set; }
}
