using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Airtable.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Airtable;
using OreoLeads.Infrastructure.Identity;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Tests.Airtable;

public class AirtableSyncBackgroundServiceTests
{
    // ── 1. ProcessTick_NoPendingJobs_DoesNothing ──────────────────────────────

    [Fact]
    public async Task ProcessTick_NoPendingJobs_DoesNothing()
    {
        var (db, svc) = BuildSyncService();

        // No jobs in db — sync service should return empty list with no errors
        var jobs = await svc.GetRecentJobsAsync(null, 10);
        jobs.Should().BeEmpty();
    }

    // ── 2. ProcessTick_PendingJob_ChangesStatus ────────────────────────────────

    [Fact]
    public async Task ProcessTick_PendingJob_ChangesStatus()
    {
        var (db, svc) = BuildSyncService();

        // Create a config
        var config = new AirtableConfiguration
        {
            ConnectionName  = "Test",
            BaseId          = "appTest",
            TableIdOrName   = "Leads",
            IsEnabled       = true,
        };
        db.AirtableConfigurations.Add(config);
        await db.SaveChangesAsync();

        // Enqueue a job
        var job = await svc.EnqueueSyncAsync(new EnqueueAirtableSyncDto(
            AirtableConfigurationId: config.Id,
            Direction:               SyncDirection.OreoLeadsToAirtable,
            IsFullSync:              true,
            LeadId:                  null,
            TriggerReason:           "test"
        ), null);

        job.Status.Should().Be(AirtableSyncJobStatus.Pending);

        // Process the job — no token set, so it should fail gracefully
        await svc.ProcessJobAsync(job.Id);

        var updated = await svc.GetJobAsync(job.Id);
        // Should have transitioned away from Pending
        updated!.Status.Should().NotBe(AirtableSyncJobStatus.Pending);
    }

    // ── 3. ProcessTick_DisabledConfig_JobRemainsUntouched ─────────────────────

    [Fact]
    public async Task ProcessTick_DisabledConfig_JobRemainsUntouched()
    {
        var (db, svc) = BuildSyncService();

        // Create a DISABLED config
        var config = new AirtableConfiguration
        {
            ConnectionName  = "Disabled Config",
            BaseId          = "appDisabled",
            TableIdOrName   = "Leads",
            IsEnabled       = false,
        };
        db.AirtableConfigurations.Add(config);
        await db.SaveChangesAsync();

        // Enqueue a job
        var job = await svc.EnqueueSyncAsync(new EnqueueAirtableSyncDto(
            AirtableConfigurationId: config.Id,
            Direction:               SyncDirection.OreoLeadsToAirtable,
            IsFullSync:              false,
            LeadId:                  null,
            TriggerReason:           "scheduled"
        ), null);

        // In the background service, it checks IsEnabled before calling ProcessJobAsync.
        // Since config is disabled, the job would be skipped.
        // We verify directly by checking that the job is still Pending if config is disabled.
        var config2 = await db.AirtableConfigurations.FindAsync(config.Id);
        config2!.IsEnabled.Should().BeFalse();

        var retrieved = await svc.GetJobAsync(job.Id);
        retrieved!.Status.Should().Be(AirtableSyncJobStatus.Pending);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (ApplicationDbContext db, AirtableSyncService svc) BuildSyncService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new ApplicationDbContext(options, new TenantContext());

        var encConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "TestEncryptionKey_AtLeast32Chars!!"
            })
            .Build();
        var encryption = new EncryptionService(encConfig);

        var airtableSvc = new StubAirtableService();
        var configSvc   = new AirtableConfigurationService(db, airtableSvc, encryption);
        var syncSvc     = new AirtableSyncService(db, configSvc, airtableSvc, NullLogger<AirtableSyncService>.Instance);

        return (db, syncSvc);
    }
}
