using OreoLeads.Application.Features.Brevo.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface IBrevoService
{
    Task<BrevoTestResultDto> TestConnectionAsync(string apiKey, CancellationToken ct = default);

    /// <summary>Sends an email via Brevo SMTP API. Returns the Brevo messageId.</summary>
    Task<string> SendEmailAsync(EmailSendRequest request, CancellationToken ct = default);

    /// <summary>Sends a transactional SMS via Brevo. Returns the Brevo messageId.</summary>
    Task<string> SendSmsAsync(SmsSendRequest request, CancellationToken ct = default);

    Task SyncContactAsync(ContactSyncRequest request, CancellationToken ct = default);
}

public record EmailSendRequest(
    string               ApiKey,
    string               SenderName,
    string               SenderEmail,
    string?              ReplyTo,
    string               ToEmail,
    string?              ToName,
    string               Subject,
    string               HtmlBody,
    Guid?                LeadId,
    IEnumerable<string>? Tags
);

public record SmsSendRequest(
    string  ApiKey,
    string  SenderName,
    string  ToPhone,
    string  Message
);

public record ContactSyncRequest(
    string               ApiKey,
    string               Email,
    string?              FirstName,
    string?              LastName,
    string?              Phone,
    string?              Company,
    string?              City,
    string?              Sector,
    IEnumerable<string>? Lists
);
