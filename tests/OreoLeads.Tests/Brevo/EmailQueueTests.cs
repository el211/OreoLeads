using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Brevo;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Brevo;

public class EmailQueueTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    // ── 1. QueueAsync_CreatesPendingJob ───────────────────────────────────────

    [Fact]
    public async Task QueueAsync_CreatesPendingJob()
    {
        await using var db  = CreateDb();
        var svc = new EmailQueueService(db);

        var generatedEmailId = Guid.NewGuid();
        var leadId           = Guid.NewGuid();

        var job = await svc.QueueAsync(
            generatedEmailId, leadId,
            "lead@example.com", "Lead Corp",
            "Subject", "<p>body</p>",
            null, null);

        job.Should().NotBeNull();
        job.Status.Should().Be(EmailSendStatus.Pending);
        job.GeneratedEmailId.Should().Be(generatedEmailId);
        job.LeadId.Should().Be(leadId);
        job.AttemptCount.Should().Be(0);
    }

    // ── 2. GetPendingAsync_ReturnsOnlyPending ──────────────────────────────────

    [Fact]
    public async Task GetPendingAsync_ReturnsOnlyPending()
    {
        await using var db  = CreateDb();
        var svc = new EmailQueueService(db);

        var leadId = Guid.NewGuid();

        await svc.QueueAsync(Guid.NewGuid(), leadId, "a@a.com", null, "S1", "<p/>", DateTime.UtcNow.AddSeconds(-1), null);
        var pending = await svc.QueueAsync(Guid.NewGuid(), leadId, "b@b.com", null, "S2", "<p/>", DateTime.UtcNow.AddSeconds(-1), null);

        // Mark first one as sent
        await svc.MarkSendingAsync(pending.Id);
        await svc.MarkSentAsync(pending.Id, "msg-id");

        var result = await svc.GetPendingAsync(10);
        result.Should().HaveCount(1);
        result.Single().ToEmail.Should().Be("a@a.com");
    }

    // ── 3. GetPendingAsync_SkipsJobsWithFutureNextAttemptAt ───────────────────

    [Fact]
    public async Task GetPendingAsync_SkipsJobsWithFutureNextAttemptAt()
    {
        await using var db  = CreateDb();
        var svc = new EmailQueueService(db);

        var job = await svc.QueueAsync(Guid.NewGuid(), Guid.NewGuid(), "x@x.com", null, "S", "<p/>", DateTime.UtcNow.AddSeconds(-1), null);

        // Simulate a failed attempt that schedules a future retry
        await svc.MarkSendingAsync(job.Id);
        await svc.MarkFailedAsync(job.Id, "timeout", canRetry: true);

        // Job should be pending again but with NextAttemptAt in the future
        var pending = await svc.GetPendingAsync(10);
        pending.Should().BeEmpty();
    }

    // ── 4. MarkSentAsync_UpdatesStatus ────────────────────────────────────────

    [Fact]
    public async Task MarkSentAsync_UpdatesStatus()
    {
        await using var db  = CreateDb();
        var svc = new EmailQueueService(db);

        var job = await svc.QueueAsync(Guid.NewGuid(), Guid.NewGuid(), "y@y.com", null, "S", "<p/>", DateTime.UtcNow.AddSeconds(-1), null);
        await svc.MarkSendingAsync(job.Id);
        await svc.MarkSentAsync(job.Id, "<brevo-msg-id>");

        var updated = await svc.GetByIdAsync(job.Id);
        updated!.Status.Should().Be(EmailSendStatus.Sent);
        updated.BrevoMessageId.Should().Be("<brevo-msg-id>");
        updated.SentAt.Should().NotBeNull();
        updated.AttemptCount.Should().Be(1);
    }

    // ── 5. MarkFailedAsync_WithRetriesLeft_SetsNextAttemptAt ─────────────────

    [Fact]
    public async Task MarkFailedAsync_WithRetriesLeft_SetsNextAttemptAt()
    {
        await using var db  = CreateDb();
        var svc = new EmailQueueService(db);

        var job = await svc.QueueAsync(Guid.NewGuid(), Guid.NewGuid(), "z@z.com", null, "S", "<p/>", DateTime.UtcNow.AddSeconds(-1), null);
        await svc.MarkSendingAsync(job.Id);
        await svc.MarkFailedAsync(job.Id, "connection error", canRetry: true);

        var updated = await svc.GetByIdAsync(job.Id);
        updated!.Status.Should().Be(EmailSendStatus.Pending);
        updated.NextAttemptAt.Should().NotBeNull();
        updated.NextAttemptAt.Should().BeAfter(DateTime.UtcNow);
        updated.AttemptCount.Should().Be(1);
    }

    // ── 6. MarkFailedAsync_NoRetriesLeft_SetsStatusFailed ────────────────────

    [Fact]
    public async Task MarkFailedAsync_NoRetriesLeft_SetsStatusFailed()
    {
        await using var db  = CreateDb();
        var svc = new EmailQueueService(db);

        var job = await svc.QueueAsync(Guid.NewGuid(), Guid.NewGuid(), "w@w.com", null, "S", "<p/>", DateTime.UtcNow.AddSeconds(-1), null);

        // Exhaust all attempts
        for (var i = 0; i < job.MaxAttempts; i++)
        {
            await svc.MarkSendingAsync(job.Id);
            await svc.MarkFailedAsync(job.Id, "persistent error", canRetry: true);
        }

        var updated = await svc.GetByIdAsync(job.Id);
        updated!.Status.Should().Be(EmailSendStatus.Failed);
    }
}
