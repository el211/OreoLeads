using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.WebUtilities;
using OreoLeads.Application.Features.Prospecting;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Brevo;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Tests.Prospecting;

public class OutreachReviewTests
{
    private static ApplicationDbContext Database() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static OutreachReviewService Service(ApplicationDbContext db)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Encryption:Key"] = "test-only-key-123456789012345678901234567890",
            ["Outreach:PublicBaseUrl"] = "https://prospecting.example.com"
        }).Build();
        return new(db, new EncryptionService(config), config);
    }

    private static OutreachReviewRequest Request() => new()
    {
        Basis = "ProfessionalLegitimateInterest", BasisEvidence = "Website maintenance relates to the recipient's business responsibility.",
        SenderIdentification = "Oreo Studios, business address, contact@example.com",
        PrivacyNoticeUrl = "https://example.com/privacy", ProfessionRelevant = true, PrivacyInformationProvided = true
    };

    private static async Task<GeneratedEmail> Draft(ApplicationDbContext db, string country = "France")
    {
        var lead = new Lead { OrganizationId = Guid.NewGuid(), CompanyName = "Business", Email = "contact@example.com", Country = country };
        var draft = new GeneratedEmail { Lead = lead, LeadId = lead.Id, OrganizationId = lead.OrganizationId,
            Subject = "Website review", Body = "A documented opportunity", RequiresOutreachReview = true };
        db.GeneratedEmails.Add(draft);
        await db.SaveChangesAsync();
        return draft;
    }

    [Theory]
    [InlineData("content")]
    [InlineData("recipient")]
    [InlineData("expiry")]
    [InlineData("approval")]
    public async Task ReviewBindsApprovalToContentRecipientAndExpiry(string change)
    {
        await using var db = Database();
        var draft = await Draft(db);
        var review = await Service(db).PrepareAsync(draft, Request(), draft.OrganizationId!.Value, "reviewer", default);
        review.ApprovedAt = DateTime.UtcNow;
        review.ApprovedByUserId = "approver";
        await db.SaveChangesAsync();
        Assert.Null(await OutreachGuard.ReviewBlockReasonAsync(db, draft, draft.Lead.Email!, true, default));
        var recipient = draft.Lead.Email!;
        if (change == "content") draft.Body += " changed";
        if (change == "recipient") recipient = "different@example.com";
        if (change == "expiry") review.ValidUntil = DateTime.UtcNow.AddSeconds(-1);
        if (change == "approval") review.ApprovedByUserId = null;
        await db.SaveChangesAsync();
        Assert.NotNull(await OutreachGuard.ReviewBlockReasonAsync(db, draft, recipient, true, default));
    }

    [Fact]
    public async Task AuthenticatedOptOutIsIdempotentAndCancelsPendingDelivery()
    {
        await using var db = Database();
        var draft = await Draft(db);
        var service = Service(db);
        var review = await service.PrepareAsync(draft, Request(), draft.OrganizationId!.Value, "reviewer", default);
        var token = QueryHelpers.ParseQuery(new Uri(review.OptOutUrl).Query)["token"].ToString();
        var payload = Assert.IsType<UnsubscribePayload>(service.ReadToken(token));
        Assert.Null(service.ReadToken(token[..^10] + "corrupted"));
        var job = new EmailSendJob { GeneratedEmailId = draft.Id, LeadId = draft.LeadId, OrganizationId = draft.OrganizationId,
            ToEmail = draft.Lead.Email!, Subject = draft.Subject, HtmlBody = draft.Body, Status = EmailSendStatus.Pending };
        db.EmailSendJobs.Add(job);
        await db.SaveChangesAsync();
        await service.UnsubscribeAsync(payload, default);
        await service.UnsubscribeAsync(payload, default);
        Assert.Single(await db.UnsubscribeRecords.ToListAsync());
        Assert.Equal(EmailSendStatus.Cancelled, job.Status);
        Assert.Contains("suppressed", await OutreachGuard.BlockReasonAsync(db, job, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PrepareAsync(draft, Request(), draft.OrganizationId.Value, "reviewer", default));
    }

    [Fact]
    public async Task UndocumentedOrUnsupportedJurisdictionCannotPrepareOutreach()
    {
        await using var db = Database();
        var draft = await Draft(db);
        var request = Request();
        request.PrivacyInformationProvided = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db).PrepareAsync(draft, request, draft.OrganizationId!.Value, "reviewer", default));
        draft.Lead.Country = "Germany";
        await db.SaveChangesAsync();
        request = Request(); request.CountryCode = "DE";
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db).PrepareAsync(draft, request, draft.OrganizationId!.Value, "reviewer", default));
        Assert.Empty(await db.OutreachReviews.ToListAsync());
    }
}
