using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Enrichment;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Enrichment;

public class EnrichmentQueueTests
{
    private static ApplicationDbContext CreateDb()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant: null);

    private static async Task<Guid> SeedLeadAsync(ApplicationDbContext db)
    {
        var lead = new Lead { CompanyName = "Test" };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        return lead.Id;
    }

    [Fact]
    public async Task Queue_IsIdempotent_WhenPendingExists()
    {
        await using var db = CreateDb();
        var leadId = await SeedLeadAsync(db);
        var svc = new EnrichmentQueueService(db);

        var first = await svc.QueueAsync(leadId, null);
        var second = await svc.QueueAsync(leadId, null);

        second.Id.Should().Be(first.Id);
        (await db.LeadEnrichments.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Queue_Force_CreatesNewJob()
    {
        await using var db = CreateDb();
        var leadId = await SeedLeadAsync(db);
        var svc = new EnrichmentQueueService(db);

        await svc.QueueAsync(leadId, null);
        await svc.QueueAsync(leadId, null, force: true);

        (await db.LeadEnrichments.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task GetPending_ReturnsOnlyDueJobs()
    {
        await using var db = CreateDb();
        var leadId = await SeedLeadAsync(db);
        var svc = new EnrichmentQueueService(db);
        await svc.QueueAsync(leadId, null);

        var pending = await svc.GetPendingAsync();
        pending.Should().HaveCount(1);
    }

    [Fact]
    public async Task MarkFailed_WithRetriesLeft_SchedulesBackoff()
    {
        await using var db = CreateDb();
        var leadId = await SeedLeadAsync(db);
        var svc = new EnrichmentQueueService(db);
        var job = await svc.QueueAsync(leadId, null);

        await svc.MarkFailedAsync(job.Id, "boom", canRetry: true);

        var reloaded = await db.LeadEnrichments.FindAsync(job.Id);
        reloaded!.Status.Should().Be(EnrichmentStatus.Pending);
        reloaded.NextAttemptAt.Should().NotBeNull().And.BeAfter(DateTime.UtcNow);
        reloaded.AttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task MarkFailed_ExhaustedAttempts_MarksFailed()
    {
        await using var db = CreateDb();
        var leadId = await SeedLeadAsync(db);
        var svc = new EnrichmentQueueService(db);
        var job = await svc.QueueAsync(leadId, null);
        job.AttemptCount = job.MaxAttempts - 1;
        await db.SaveChangesAsync();

        await svc.MarkFailedAsync(job.Id, "boom", canRetry: true);

        (await db.LeadEnrichments.FindAsync(job.Id))!.Status.Should().Be(EnrichmentStatus.Failed);
    }

    [Fact]
    public async Task MarkRunning_Then_Completed_TransitionsStatus()
    {
        await using var db = CreateDb();
        var leadId = await SeedLeadAsync(db);
        var svc = new EnrichmentQueueService(db);
        var job = await svc.QueueAsync(leadId, null);

        await svc.MarkRunningAsync(job.Id);
        (await db.LeadEnrichments.FindAsync(job.Id))!.Status.Should().Be(EnrichmentStatus.Running);

        await svc.MarkCompletedAsync(job.Id);
        (await db.LeadEnrichments.FindAsync(job.Id))!.Status.Should().Be(EnrichmentStatus.Completed);
    }
}
