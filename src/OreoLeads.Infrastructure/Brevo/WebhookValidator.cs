using System.Security.Cryptography;
using System.Text;

namespace OreoLeads.Infrastructure.Brevo;

/// <summary>Validates incoming Brevo webhook requests via a pre-shared secret key.</summary>
public static class WebhookValidator
{
    /// <summary>
    /// Returns true when:
    ///   - no secret is configured (open accept), or
    ///   - the provided header value matches the configured secret (constant-time comparison).
    /// Throws <see cref="UnauthorizedAccessException"/> on mismatch.
    /// </summary>
    public static void Validate(string? configuredSecret, string? headerValue)
    {
        // No secret configured → accept everything
        if (string.IsNullOrWhiteSpace(configuredSecret))
            return;

        if (string.IsNullOrWhiteSpace(headerValue))
            throw new UnauthorizedAccessException("Missing X-Brevo-Webhook-Key header.");

        // Constant-time comparison to prevent timing attacks
        var secretBytes = Encoding.UTF8.GetBytes(configuredSecret);
        var headerBytes = Encoding.UTF8.GetBytes(headerValue);

        if (!CryptographicOperations.FixedTimeEquals(secretBytes, headerBytes))
            throw new UnauthorizedAccessException("Invalid X-Brevo-Webhook-Key header.");
    }

    /// <summary>Maps the Brevo event string (e.g. "delivered") to <see cref="OreoLeads.Domain.Enums.EmailEventType"/>.</summary>
    public static Domain.Enums.EmailEventType MapEventType(string? eventString)
        => (eventString ?? string.Empty).ToLowerInvariant() switch
        {
            "queued"        => Domain.Enums.EmailEventType.Queued,
            "sent"          => Domain.Enums.EmailEventType.Sent,
            "delivered"     => Domain.Enums.EmailEventType.Delivered,
            "opened"        => Domain.Enums.EmailEventType.Opened,
            "clicked"
             or "click"     => Domain.Enums.EmailEventType.Clicked,
            "softbounce"
             or "soft_bounce" => Domain.Enums.EmailEventType.SoftBounce,
            "hardbounce"
             or "hard_bounce" => Domain.Enums.EmailEventType.HardBounce,
            "spam"
             or "spamreport" => Domain.Enums.EmailEventType.Spam,
            "blocked"       => Domain.Enums.EmailEventType.Blocked,
            "invalid_email"
             or "invalid"   => Domain.Enums.EmailEventType.Invalid,
            "deferred"      => Domain.Enums.EmailEventType.Deferred,
            "unsubscribed"
             or "unsubscribe" => Domain.Enums.EmailEventType.Unsubscribed,
            "reply"         => Domain.Enums.EmailEventType.Reply,
            _               => Domain.Enums.EmailEventType.Queued
        };
}
