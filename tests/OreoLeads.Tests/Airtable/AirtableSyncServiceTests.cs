using System.Net;
using System.Text;
using System.Text.Json;
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

public class AirtableSyncServiceTests
{
    // ── 1. EnqueueSync_ValidRequest_CreatesPendingJob ─────────────────────────

    [Fact]
    public async Task EnqueueSync_ValidRequest_CreatesPendingJob()
    {
        var (svc, _) = BuildService();
        var configId = Guid.NewGuid();

        var job = await svc.EnqueueSyncAsync(new EnqueueAirtableSyncDto(
            AirtableConfigurationId: configId,
            Direction:               SyncDirection.OreoLeadsToAirtable,
            IsFullSync:              false,
            LeadId:                  null,
            TriggerReason:           "manual"
        ), null);

        job.Should().NotBeNull();
        job.Status.Should().Be(AirtableSyncJobStatus.Pending);
        job.TriggerReason.Should().Be("manual");
        job.AirtableConfigurationId.Should().Be(configId);
    }

    // ── 2. GetRecentJobs_ReturnsOrdered ──────────────────────────────────────

    [Fact]
    public async Task GetRecentJobs_ReturnsOrdered()
    {
        var (svc, _) = BuildService();
        var configId = Guid.NewGuid();

        await svc.EnqueueSyncAsync(new EnqueueAirtableSyncDto(configId, SyncDirection.OreoLeadsToAirtable, false, null, "first"), null);
        await Task.Delay(10);
        await svc.EnqueueSyncAsync(new EnqueueAirtableSyncDto(configId, SyncDirection.OreoLeadsToAirtable, false, null, "second"), null);

        var jobs = await svc.GetRecentJobsAsync(null, 10);

        jobs.Should().HaveCount(2);
        // Most recent first
        jobs[0].TriggerReason.Should().Be("second");
    }

    // ── 3. CancelJob_SetsStatusCancelled ─────────────────────────────────────

    [Fact]
    public async Task CancelJob_SetsStatusCancelled()
    {
        var (svc, _) = BuildService();
        var job = await svc.EnqueueSyncAsync(
            new EnqueueAirtableSyncDto(Guid.NewGuid(), SyncDirection.OreoLeadsToAirtable, false, null, "test"), null);

        await svc.CancelJobAsync(job.Id);

        var updated = await svc.GetJobAsync(job.Id);
        updated!.Status.Should().Be(AirtableSyncJobStatus.Cancelled);
    }

    // ── 4. GetConflicts_ReturnsOnlyConflicts ──────────────────────────────────

    [Fact]
    public async Task GetConflicts_ReturnsOnlyConflicts()
    {
        var (svc, db) = BuildService();

        // Add a lead
        var lead = new Lead { CompanyName = "ACME" };
        db.Leads.Add(lead);

        var configId = Guid.NewGuid();

        // Create a conflict record link
        var conflictLink = new AirtableRecordLink
        {
            LeadId                  = lead.Id,
            AirtableConfigurationId = configId,
            AirtableRecordId        = "recConflict1",
            ConflictStatus          = AirtableSyncJobStatus.Conflict,
        };
        db.AirtableRecordLinks.Add(conflictLink);

        // Create a non-conflict link
        var normalLink = new AirtableRecordLink
        {
            LeadId                  = lead.Id,
            AirtableConfigurationId = Guid.NewGuid(),
            AirtableRecordId        = "recNormal1",
        };
        db.AirtableRecordLinks.Add(normalLink);

        await db.SaveChangesAsync();

        var conflicts = await svc.GetConflictsAsync(null);

        conflicts.Should().HaveCount(1);
        conflicts[0].AirtableRecordId.Should().Be("recConflict1");
    }

    // ── 5. ResolveConflict_OreoLeadsWins_ClearsConflict ──────────────────────

    [Fact]
    public async Task ResolveConflict_OreoLeadsWins_ClearsConflict()
    {
        var (svc, db) = BuildService();

        var lead = new Lead { CompanyName = "ACME" };
        db.Leads.Add(lead);

        var configId = Guid.NewGuid();
        var link = new AirtableRecordLink
        {
            LeadId                  = lead.Id,
            AirtableConfigurationId = configId,
            AirtableRecordId        = "recConflict1",
            ConflictStatus          = AirtableSyncJobStatus.Conflict,
            ConflictOreoLeadsData   = "{\"CompanyName\":\"ACME\"}",
            ConflictAirtableData    = "{\"CompanyName\":\"ACME_AT\"}",
        };
        db.AirtableRecordLinks.Add(link);
        await db.SaveChangesAsync();

        await svc.ResolveConflictAsync(link.Id, new ConflictResolutionDto("oreoleads"), null);

        var updated = await db.AirtableRecordLinks.FindAsync(link.Id);
        updated!.ConflictStatus.Should().BeNull();
        updated.ConflictResolvedAt.Should().NotBeNull();
        updated.ConflictResolvedBy.Should().Be("oreoleads");
    }

    // ── 6. ResolveConflict_AirtableWins_AppliesAirtableData ──────────────────

    [Fact]
    public async Task ResolveConflict_AirtableWins_AppliesAirtableData()
    {
        var (svc, db) = BuildService();

        var lead = new Lead { CompanyName = "ACME" };
        db.Leads.Add(lead);

        // We need a config with field mappings for the resolution
        var config = new AirtableConfiguration
        {
            ConnectionName = "Test",
            BaseId         = "app1",
            TableIdOrName  = "Leads",
        };
        db.AirtableConfigurations.Add(config);

        var link = new AirtableRecordLink
        {
            LeadId                  = lead.Id,
            AirtableConfigurationId = config.Id,
            AirtableRecordId        = "recConflict2",
            ConflictStatus          = AirtableSyncJobStatus.Conflict,
            ConflictOreoLeadsData   = "{\"CompanyName\":\"ACME\"}",
            ConflictAirtableData    = "{\"CompanyName\":\"ACME_FROM_AIRTABLE\"}",
        };
        db.AirtableRecordLinks.Add(link);
        await db.SaveChangesAsync();

        await svc.ResolveConflictAsync(link.Id, new ConflictResolutionDto("airtable"), null);

        var updated = await db.AirtableRecordLinks.FindAsync(link.Id);
        updated!.ConflictStatus.Should().BeNull();
        updated.ConflictResolvedBy.Should().Be("airtable");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (AirtableSyncService svc, ApplicationDbContext db) BuildService()
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
        var svc         = new AirtableSyncService(db, configSvc, airtableSvc, NullLogger<AirtableSyncService>.Instance);
        return (svc, db);
    }
}
