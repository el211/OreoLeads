using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace OreoLeads.Infrastructure.Smtp;

/// <summary>
/// Sends emails via standard SMTP using MailKit.
/// Supports implicit SSL (port 465) and STARTTLS (port 587).
/// Compatible with OVH, Namecheap Private Email, Gmail, etc.
/// </summary>
public sealed class SmtpEmailSender
{
    private readonly SmtpSettings             _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger   = logger;
    }

    public bool IsConfigured => _settings.IsConfigured;

    /// <summary>Sends an HTML email. Returns a pseudo message-id.</summary>
    public async Task<string> SendAsync(
        string            toEmail,
        string?           toName,
        string            subject,
        string            htmlBody,
        CancellationToken ct = default)
    {
        var message = BuildMessage(toEmail, toName, subject, htmlBody);

        _logger.LogInformation(
            "SMTP sending to {ToEmail} via {Host}:{Port} (SSL={Ssl})",
            toEmail, _settings.Host, _settings.Port, _settings.UseSsl);

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _settings.Host!,
            _settings.Port,
            _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable,
            ct);

        await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(quit: true, ct);

        var messageId = message.MessageId ?? $"smtp-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        _logger.LogInformation("SMTP email sent to {ToEmail}. Id={MessageId}", toEmail, messageId);
        return messageId;
    }

    /// <summary>Tests SMTP connectivity by sending a test email to the sender address.</summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            await SendAsync(
                _settings.SenderEmail!,
                _settings.SenderName,
                "[OreoLeads] Test de connexion SMTP",
                "<p>Connexion SMTP vérifiée avec succès depuis OreoLeads.</p>",
                ct);
            return (true, $"Connexion réussie via {_settings.Host}:{_settings.Port}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP test connection failed");
            return (false, ex.Message);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private MimeMessage BuildMessage(string toEmail, string? toName, string subject, string htmlBody)
    {
        var senderName  = _settings.SenderName  ?? _settings.SenderEmail!;
        var senderEmail = _settings.SenderEmail!;

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(senderName, senderEmail));
        msg.To.Add(string.IsNullOrWhiteSpace(toName)
            ? new MailboxAddress(toEmail, toEmail)
            : new MailboxAddress(toName, toEmail));
        msg.Subject = subject;
        msg.Body    = new TextPart("html") { Text = htmlBody };
        return msg;
    }
}
