using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OreoLeads.Infrastructure.Smtp;

/// <summary>
/// Sends emails via standard SMTP (OVH, Namecheap Private Email, Gmail, etc.)
/// using the built-in System.Net.Mail.SmtpClient.
/// </summary>
public sealed class SmtpEmailSender
{
    private readonly SmtpSettings                  _settings;
    private readonly ILogger<SmtpEmailSender>      _logger;

    public SmtpEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger   = logger;
    }

    public bool IsConfigured => _settings.IsConfigured;

    /// <summary>Sends an email. Returns a pseudo message-id (timestamp-based).</summary>
    public async Task<string> SendAsync(
        string  toEmail,
        string? toName,
        string  subject,
        string  htmlBody,
        CancellationToken ct = default)
    {
        using var client = BuildClient();
        using var msg    = BuildMessage(toEmail, toName, subject, htmlBody);

        _logger.LogInformation(
            "SMTP sending to {ToEmail} via {Host}:{Port} (SSL={Ssl})",
            toEmail, _settings.Host, _settings.Port, _settings.UseSsl);

        await client.SendMailAsync(msg, ct);

        var messageId = $"smtp-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        _logger.LogInformation("SMTP email sent to {ToEmail}. Id={MessageId}", toEmail, messageId);
        return messageId;
    }

    /// <summary>Tests the SMTP connection by authenticating without sending.</summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            using var client = BuildClient();
            // Send to self as a connectivity probe (some servers reject NOOP, send a real message)
            using var msg = BuildMessage(
                _settings.SenderEmail!,
                _settings.SenderName,
                "[OreoLeads] Test de connexion SMTP",
                "<p>Connexion SMTP vérifiée avec succès depuis OreoLeads.</p>");

            await client.SendMailAsync(msg, ct);
            return (true, $"Connexion réussie via {_settings.Host}:{_settings.Port}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP test connection failed");
            return (false, ex.Message);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private SmtpClient BuildClient()
    {
        var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl            = _settings.UseSsl,
            DeliveryMethod       = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials          = new NetworkCredential(_settings.Username, _settings.Password),
            Timeout              = 30_000, // 30 s
        };
        return client;
    }

    private MailMessage BuildMessage(string toEmail, string? toName, string subject, string htmlBody)
    {
        var senderName  = _settings.SenderName  ?? _settings.SenderEmail!;
        var senderEmail = _settings.SenderEmail!;

        var msg = new MailMessage
        {
            From       = new MailAddress(senderEmail, senderName),
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true,
        };

        msg.To.Add(string.IsNullOrWhiteSpace(toName)
            ? new MailAddress(toEmail)
            : new MailAddress(toEmail, toName));

        return msg;
    }
}
