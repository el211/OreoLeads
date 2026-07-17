using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Brevo;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Brevo;

public class EmailStatsTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    // ── 1. GetStatsAsync_NoData_ReturnsZeros ──────────────────────────────────

    [Fact]
    public async Task GetStatsAsync_NoData_ReturnsZeros()
    {
        await using var db  = CreateDb();
        var svc = new EmailStatsService(db);

        var stats = await svc.GetStatsAsync();

        stats.TotalSent.Should().Be(0);
        stats.TotalDelivered.Should().Be(0);
        stats.TotalOpened.Should().Be(0);
        stats.TotalClicked.Should().Be(0);
        stats.TotalBounced.Should().Be(0);
        stats.OpenRate.Should().Be(0);
        stats.ClickRate.Should().Be(0);
        stats.BounceRate.Should().Be(0);
        stats.ReplyCount.Should().Be(0);
    }

    // ── 2. GetStatsAsync_WithSentJobs_CalculatesRates ─────────────────────────

    [Fact]
    public async Task GetStatsAsync_WithSentJobs_CalculatesRates()
    {
        await using var db  = CreateDb();
        var svc = new EmailStatsService(db);

        // Add 4 sent jobs
        for (var i = 0; i < 4; i++)
            db.Set<EmailSendJob>().Add(BuildJob(EmailSendStatus.Sent));

        // Add 1 pending (should not count)
        db.Set<EmailSendJob>().Add(BuildJob(EmailSendStatus.Pending));

        await db.SaveChangesAsync();

        var stats = await svc.GetStatsAsync();
        stats.TotalSent.Should().Be(4);
    }

    // ── 3. GetStatsAsync_WithEvents_CountsCorrectly ───────────────────────────

    [Fact]
    public async Task GetStatsAsync_WithEvents_CountsCorrectly()
    {
        await using var db  = CreateDb();
        var svc = new EmailStatsService(db);

        var job = BuildJob(EmailSendStatus.Sent);
        db.Set<EmailSendJob>().Add(job);
        await db.SaveChangesAsync();

        // 2 delivered, 1 opened, 1 clicked, 1 hard bounce, 1 reply
        db.Set<EmailEvent>().Add(BuildEvent(job.Id, EmailEventType.Delivered));
        db.Set<EmailEvent>().Add(BuildEvent(job.Id, EmailEventType.Delivered));
        db.Set<EmailEvent>().Add(BuildEvent(job.Id, EmailEventType.Opened));
        db.Set<EmailEvent>().Add(BuildEvent(job.Id, EmailEventType.Clicked));
        db.Set<EmailEvent>().Add(BuildEvent(job.Id, EmailEventType.HardBounce));
        db.Set<EmailEvent>().Add(BuildEvent(job.Id, EmailEventType.Reply));
        await db.SaveChangesAsync();

        var stats = await svc.GetStatsAsync();

        stats.TotalSent.Should().Be(1);
        stats.TotalDelivered.Should().Be(2);
        stats.TotalOpened.Should().Be(1);
        stats.TotalClicked.Should().Be(1);
        stats.TotalBounced.Should().Be(1);
        stats.OpenRate.Should().Be(100d);
        stats.ClickRate.Should().Be(100d);
        stats.BounceRate.Should().Be(100d);
        stats.ReplyCount.Should().Be(1);
    }

    // ── 4. GetStatsAsync_WithOrganizationFilter_FiltersCorrectly ─────────────

    [Fact]
    public async Task GetStatsAsync_WithOrganizationFilter_FiltersCorrectly()
    {
        await using var db  = CreateDb();
        var svc = new EmailStatsService(db);

        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        db.Set<EmailSendJob>().Add(BuildJob(EmailSendStatus.Sent, orgA));
        db.Set<EmailSendJob>().Add(BuildJob(EmailSendStatus.Sent, orgA));
        db.Set<EmailSendJob>().Add(BuildJob(EmailSendStatus.Sent, orgB));
        await db.SaveChangesAsync();

        var statsA = await svc.GetStatsAsync(organizationId: orgA);
        var statsB = await svc.GetStatsAsync(organizationId: orgB);

        statsA.TotalSent.Should().Be(2);
        statsB.TotalSent.Should().Be(1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static EmailSendJob BuildJob(EmailSendStatus status, Guid? orgId = null) => new()
    {
        GeneratedEmailId = Guid.NewGuid(),
        LeadId           = Guid.NewGuid(),
        Status           = status,
        ScheduledAt      = DateTime.UtcNow,
        ToEmail          = "test@example.com",
        Subject          = "Test",
        HtmlBody         = "<p>test</p>",
        OrganizationId   = orgId
    };

    private static EmailEvent BuildEvent(Guid jobId, EmailEventType type) => new()
    {
        EmailSendJobId = jobId,
        EventType      = type,
        OccurredAt     = DateTime.UtcNow
    };
}
