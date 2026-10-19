using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Prospecting;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Services;

namespace OreoLeads.Infrastructure.Brevo;

public sealed class OutreachReviewService(ApplicationDbContext db, IEncryptionService encryption, IConfiguration configuration)
{
    private const string FooterMarker = "<!-- oreoleads-outreach-footer -->";
    public static string ContentHash(string subject, string body, string recipient) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { subject, body, recipient.Trim().ToLowerInvariant() }))));

    public async Task<OutreachReview> PrepareAsync(GeneratedEmail draft, OutreachReviewRequest request, Guid organizationId, string userId, CancellationToken ct)
    {
        if (!HasSecureTokenKey()) throw new InvalidOperationException("Configure a non-default Encryption:Key of at least 32 characters for unsubscribe tokens.");
        if (draft.OrganizationId != organizationId) throw new InvalidOperationException("Organization mismatch.");
        if (draft.Status == EmailStatus.Sent) throw new InvalidOperationException("Sent drafts cannot be changed.");
        if (await db.EmailSendJobs.AnyAsync(j => j.GeneratedEmailId == draft.Id && j.Status == EmailSendStatus.Sending, ct))
            throw new InvalidOperationException("Delivery is already in progress; reconcile it before changing the review.");
        var lead = await db.Leads.SingleAsync(l => l.Id == draft.LeadId && l.OrganizationId == organizationId, ct);
        var email = lead.Email?.Trim().ToLowerInvariant();
        if (email == null || !System.Net.Mail.MailAddress.TryCreate(email, out _)) throw new InvalidOperationException("A documented recipient email is required.");
        if (await db.UnsubscribeRecords.AnyAsync(u => u.Email.Trim().ToLower() == email, ct)) throw new InvalidOperationException("Recipient is suppressed.");
        var country = NormalizeCountry(lead.Country);
        if (country != request.CountryCode) throw new InvalidOperationException("Review jurisdiction must match the lead country.");
        var allowed = configuration.GetSection("Outreach:AllowedCountries").Get<string[]>() ?? ["FR"];
        if (!allowed.Contains(country, StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException("Outreach is disabled for this jurisdiction.");
        if (!request.ProfessionRelevant || !request.PrivacyInformationProvided)
            throw new InvalidOperationException("Document professional relevance and provision of privacy information before preparing outreach.");
        if (request.Basis == "ProfessionalLegitimateInterest" &&
            (country != "FR" || !configuration.GetValue("Outreach:AllowFrenchProfessionalLegitimateInterest", true)))
            throw new InvalidOperationException("This jurisdiction policy requires documented consent.");
        if (request.Basis is not ("Consent" or "ProfessionalLegitimateInterest")) throw new InvalidOperationException("Unknown outreach basis.");
        if (request.BasisEvidence.Trim().Length < 20 || request.SenderIdentification.Trim().Length < 10)
            throw new InvalidOperationException("Document the basis and full sender identification.");
        if (!Uri.TryCreate(request.PrivacyNoticeUrl, UriKind.Absolute, out var privacy) || privacy.Scheme != "https" || privacy.UserInfo.Length != 0)
            throw new InvalidOperationException("A public HTTPS privacy notice is required.");
        if (!Uri.TryCreate(configuration["Outreach:PublicBaseUrl"], UriKind.Absolute, out var origin) || origin.Scheme != "https" || origin.UserInfo.Length != 0)
            throw new InvalidOperationException("Configure Outreach:PublicBaseUrl with the public HTTPS application URL.");

        var payload = JsonSerializer.Serialize(new UnsubscribePayload("oreoleads:unsubscribe:v1", organizationId, lead.Id, email));
        var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(encryption.Encrypt(payload)));
        var optOutUrl = new Uri(origin, "/api/outreach/unsubscribe?token=" + token).AbsoluteUri;
        var review = await db.OutreachReviews.SingleOrDefaultAsync(r => r.EmailDraftId == draft.Id, ct);
        if (review == null) { review = new() { EmailDraftId = draft.Id, OrganizationId = organizationId }; db.OutreachReviews.Add(review); }
        review.RecipientEmail = email;
        review.CountryCode = country!;
        review.Basis = request.Basis;
        review.BasisEvidence = request.BasisEvidence.Trim();
        review.SenderIdentification = request.SenderIdentification.Trim();
        review.PrivacyNoticeUrl = privacy.AbsoluteUri;
        review.OptOutUrl = optOutUrl;
        review.ReviewedByUserId = userId;
        review.ReviewedAt = DateTime.UtcNow;
        review.ValidUntil = DateTime.UtcNow.AddDays(Math.Clamp(request.ValidDays, 1, 90));
        review.ApprovedAt = null;
        review.ApprovedByUserId = null;
        var content = draft.Body;
        var marker = content.LastIndexOf(FooterMarker, StringComparison.Ordinal);
        if (marker >= 0) content = content[..marker];
        var (privacyLabel, optOutLabel) = draft.Language switch
        {
            "en" => ("Privacy information", "Unsubscribe from business outreach"),
            "es" => ("Información de privacidad", "Cancelar comunicaciones comerciales"),
            "de" => ("Datenschutzhinweise", "Werbliche E-Mails abbestellen"),
            _ => ("Informations de confidentialité", "Ne plus recevoir de prospection")
        };
        draft.Body = EmailBodyFormatter.EnsureHtml(content) + FooterMarker +
            $"<hr><p>{WebUtility.HtmlEncode(review.SenderIdentification)}</p><p><a href=\"{WebUtility.HtmlEncode(privacy.AbsoluteUri)}\">{privacyLabel}</a> · <a href=\"{WebUtility.HtmlEncode(optOutUrl)}\">{optOutLabel}</a></p>";
        draft.Status = EmailStatus.Edited;
        draft.RequiresOutreachReview = true;
        draft.CurrentVersion++;
        db.EmailDraftVersions.Add(new() { EmailDraftId = draft.Id, Version = draft.CurrentVersion, Subject = draft.Subject, Body = draft.Body, ProviderUsed = "Human review", ModelUsed = "" });
        draft.SetUpdatedAt();
        review.ContentHash = ContentHash(draft.Subject, draft.Body, email);
        db.AuditLogs.Add(new() { OrganizationId = organizationId, UserId = userId, Action = "OutreachEligibilityReviewed", EntityName = nameof(GeneratedEmail), EntityId = draft.Id.ToString(), NewValues = JsonSerializer.Serialize(new { review.CountryCode, review.Basis, review.ContentHash, review.ValidUntil }) });
        await db.SaveChangesAsync(ct);
        return review;
    }

    public UnsubscribePayload? ReadToken(string token)
    {
        if (token.Length > 4096 || !HasSecureTokenKey()) return null;
        try
        {
            var payload = JsonSerializer.Deserialize<UnsubscribePayload>(encryption.Decrypt(Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token))));
            return payload?.Purpose == "oreoleads:unsubscribe:v1" && System.Net.Mail.MailAddress.TryCreate(payload.Email, out _) ? payload : null;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or JsonException or ArgumentException) { return null; }
    }

    public async Task UnsubscribeAsync(UnsubscribePayload payload, CancellationToken ct)
    {
        // Existing suppression is global by address. Never re-enable an address automatically.
        var email = payload.Email.Trim().ToLowerInvariant();
        if (db.Database.IsRelational())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO unsubscribe_records ("Id", "Email", "UnsubscribedAt", "Source", "Reason", "CreatedAt")
                VALUES ({Guid.NewGuid()}, {email}, {DateTime.UtcNow}, 'self-service', 'Recipient opted out', {DateTime.UtcNow})
                ON CONFLICT ("Email") DO NOTHING
                """, ct);
        }
        else if (!await db.UnsubscribeRecords.AnyAsync(r => r.Email.Trim().ToLower() == email, ct))
            db.UnsubscribeRecords.Add(new() { Email = email, Source = "self-service", Reason = "Recipient opted out" });
        var pending = await db.EmailSendJobs.Where(j => j.ToEmail.Trim().ToLower() == email && j.Status == EmailSendStatus.Pending).ToListAsync(ct);
        foreach (var job in pending) { job.Status = EmailSendStatus.Cancelled; job.ErrorMessage = "Recipient opted out"; job.SetUpdatedAt(); }
        db.AuditLogs.Add(new() { OrganizationId = payload.OrganizationId, Action = "RecipientUnsubscribed", EntityName = nameof(Lead), EntityId = payload.LeadId.ToString() });
        await db.SaveChangesAsync(ct);
    }

    private bool HasSecureTokenKey() => configuration["Encryption:Key"] is { Length: >= 32 } key && key != "OreoLeadsDefaultEncryptionKey32!";

    public static string? NormalizeCountry(string? country) => country?.Trim().ToUpperInvariant() switch
    {
        "FR" or "FRANCE" => "FR", "DE" or "GERMANY" or "ALLEMAGNE" or "DEUTSCHLAND" => "DE",
        "ES" or "SPAIN" or "ESPAGNE" or "ESPAÑA" => "ES", "GB" or "UK" or "UNITED KINGDOM" or "ROYAUME-UNI" => "GB",
        "US" or "USA" or "UNITED STATES" => "US", _ => null
    };
}

public record UnsubscribePayload(string Purpose, Guid OrganizationId, Guid LeadId, string Email);
