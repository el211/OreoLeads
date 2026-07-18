namespace OreoLeads.Infrastructure.Smtp;

public sealed class SmtpSettings
{
    public const string Section = "Smtp";

    public string? Host        { get; set; }
    public int     Port        { get; set; } = 587;
    /// <summary>true = implicit SSL (port 465). false = STARTTLS (port 587).</summary>
    public bool    UseSsl      { get; set; } = false;
    public string? Username    { get; set; }
    public string? Password    { get; set; }
    public string? SenderName  { get; set; }
    public string? SenderEmail { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)        &&
        !string.IsNullOrWhiteSpace(Username)    &&
        !string.IsNullOrWhiteSpace(Password)    &&
        !string.IsNullOrWhiteSpace(SenderEmail);
}
