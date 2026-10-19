using Microsoft.EntityFrameworkCore;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Brevo;

public static class OutreachGuard
{
    public static async Task<string?> BlockReasonAsync(ApplicationDbContext db, EmailSendJob job, CancellationToken ct, int frequencyCapDays = 7)
    {
        var normalized = job.ToEmail.Trim().ToLowerInvariant();
        if (await db.UnsubscribeRecords.AnyAsync(u => u.Email.Trim().ToLower() == normalized, ct))
            return "Recipient suppressed (unsubscribe, complaint or hard bounce).";
        var draft = await db.GeneratedEmails.FirstOrDefaultAsync(e => e.Id == job.GeneratedEmailId && e.LeadId == job.LeadId, ct);
        if (draft == null || draft.Status != EmailStatus.Approved) return "Current draft requires human approval.";
        if (draft.Subject != job.Subject || draft.Body != job.HtmlBody) return "Draft changed after queueing; approve and queue the current content.";
        if (draft.OrganizationId != job.OrganizationId) return "Organization mismatch.";
        var reviewBlock = await ReviewBlockReasonAsync(db, draft, job.ToEmail, true, ct);
        if (reviewBlock != null) return reviewBlock;
        var cutoff = DateTime.UtcNow.AddDays(-Math.Clamp(frequencyCapDays, 1, 365));
        if (await db.EmailSendJobs.AnyAsync(j => j.Id != job.Id && j.OrganizationId == job.OrganizationId &&
            j.ToEmail.Trim().ToLower() == normalized && j.Status == EmailSendStatus.Sent && j.SentAt >= cutoff, ct))
            return "Recipient frequency cap reached.";
        return null;
    }

    public static async Task<string?> ReviewBlockReasonAsync(ApplicationDbContext db, GeneratedEmail draft, string recipient, bool requireApproval, CancellationToken ct)
    {
        if (!draft.RequiresOutreachReview) return null;
        var review = await db.OutreachReviews.SingleOrDefaultAsync(r => r.EmailDraftId == draft.Id && r.OrganizationId == draft.OrganizationId, ct);
        if (review == null) return "Document outreach eligibility and prepare sender/opt-out information before approval.";
        if (review.ValidUntil <= DateTime.UtcNow) return "Outreach eligibility review has expired.";
        if (!string.Equals(review.RecipientEmail, recipient.Trim(), StringComparison.OrdinalIgnoreCase)) return "Recipient changed after eligibility review.";
        if (review.ContentHash != OutreachReviewService.ContentHash(draft.Subject, draft.Body, recipient)) return "Content changed after eligibility review.";
        if (requireApproval && (review.ApprovedAt == null || string.IsNullOrWhiteSpace(review.ApprovedByUserId))) return "Human approval of the reviewed outreach is required.";
        return null;
    }
}
