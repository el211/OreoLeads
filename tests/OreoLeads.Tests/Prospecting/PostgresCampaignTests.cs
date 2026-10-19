using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Search.DTOs;
using OreoLeads.Infrastructure.Services;
using OreoLeads.Infrastructure.Enrichment;
using OreoLeads.Infrastructure.Prospecting;
using OreoLeads.Infrastructure.Persistence.Repositories;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Prospecting;

public class PostgresCampaignTests
{
    [PostgresFact]
    public async Task MigrationAndRollbackRecoveryOnIsolatedDatabase()
    {
        var connection = Environment.GetEnvironmentVariable("OREOLEADS_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("PostgreSQL test connection required.");
        var name = "oreoleads_test_" + Guid.NewGuid().ToString("N");
        var adminBuilder = new NpgsqlConnectionStringBuilder(connection) { Database = "postgres" };
        await using var admin = new NpgsqlConnection(adminBuilder.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE {name}", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connection) { Database = name };
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(builder.ConnectionString)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)).Options;
            await using (var db = new ApplicationDbContext(options))
            {
                await db.Database.MigrateAsync();
                var campaign = new ProspectingCampaign { OrganizationId = Guid.NewGuid(), Name = "Recovery", Status = "Active" };
                db.ProspectingCampaigns.Add(campaign);
                await db.SaveChangesAsync();
                await using (var transaction = await db.Database.BeginTransactionAsync())
                {
                    await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(713980124)");
                    campaign.NextPage = 2;
                    db.Leads.Add(new() { CompanyName = "Uncommitted", OrganizationId = campaign.OrganizationId });
                    await db.SaveChangesAsync();
                    await transaction.RollbackAsync();
                }
                db.ChangeTracker.Clear();
                Assert.Equal(1, (await db.ProspectingCampaigns.SingleAsync()).NextPage);
                Assert.Empty(await db.Leads.ToListAsync());
            }
            await using (var restarted = new ApplicationDbContext(options))
                Assert.Equal("Active", (await restarted.ProspectingCampaigns.SingleAsync()).Status);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddScoped<TenantContext>();
            services.AddScoped(sp => new ApplicationDbContext(options, sp.GetRequiredService<TenantContext>()));
            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<ISearchRepository, SearchRepository>();
            services.AddScoped<IEnrichmentQueueService, EnrichmentQueueService>();
            services.AddSingleton<ILeadSource, RegistryFixture>();
            await using var provider = services.BuildServiceProvider();
            var worker = new CampaignWorker(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<CampaignWorker>.Instance, new ConfigurationBuilder().Build());
            Guid campaignId;
            await using (var db = new ApplicationDbContext(options))
            {
                var campaign = await db.ProspectingCampaigns.SingleAsync();
                campaign.DailyLimit = 1;
                campaignId = campaign.Id;
                await db.SaveChangesAsync();
            }
            await worker.ProcessAsync(campaignId, default);
            await using (var db = new ApplicationDbContext(options))
            {
                Assert.Single(await db.Leads.ToListAsync());
                Assert.Single(await db.LeadEnrichments.ToListAsync());
                var campaign = await db.ProspectingCampaigns.SingleAsync();
                Assert.Equal(1, campaign.PageOffset);
                campaign.QuotaDate = DateTime.UtcNow.Date.AddDays(-1);
                campaign.NextRunAt = DateTime.UtcNow.AddSeconds(-1);
                campaign.NextDiscoveryAt = DateTime.UtcNow.AddSeconds(-1);
                await db.SaveChangesAsync();
            }
            // A fresh worker recovers the durable cursor and processes the remainder of the page.
            var restartedWorker = new CampaignWorker(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<CampaignWorker>.Instance, new ConfigurationBuilder().Build());
            await restartedWorker.ProcessAsync(campaignId, default);
            await using (var db = new ApplicationDbContext(options))
            {
                Assert.Equal(2, await db.Leads.CountAsync());
                Assert.Equal(2, await db.LeadEnrichments.CountAsync());
                var campaign = await db.ProspectingCampaigns.SingleAsync();
                campaign.NextPage = 1;
                campaign.QuotaDate = DateTime.UtcNow.Date.AddDays(-1);
                campaign.NextRunAt = DateTime.UtcNow.AddSeconds(-1);
                campaign.NextDiscoveryAt = DateTime.UtcNow.AddSeconds(-1);
                await db.SaveChangesAsync();
            }
            await restartedWorker.ProcessAsync(campaignId, default);
            await using (var db = new ApplicationDbContext(options))
            {
                Assert.Equal(2, await db.Leads.CountAsync());
                Assert.Equal(2, await db.LeadEnrichments.CountAsync());
                Assert.Equal(2, await db.CampaignProspects.CountAsync());
                var prospect = await db.CampaignProspects.FirstAsync();
                prospect.QualifiedAt = DateTime.UtcNow;
                prospect.AiLeaseUntil = DateTime.UtcNow.AddMinutes(-1);
                prospect.AiLeaseToken = Guid.NewGuid();
                await db.SaveChangesAsync();
                var ai = new RecommendationFixture();
                var recommendations = new CampaignRecommendationService(db, ai);
                await Assert.ThrowsAsync<InvalidOperationException>(() => recommendations.GenerateAsync(prospect.Id, prospect.OrganizationId, default));
                db.ChangeTracker.Clear();
                var failed = await db.CampaignProspects.SingleAsync(p => p.Id == prospect.Id);
                Assert.Equal(1, failed.AiAttempts);
                Assert.NotNull(failed.NextAiAttemptAt);
                Assert.Null(failed.AiLeaseToken);
                await Assert.ThrowsAsync<InvalidOperationException>(() => recommendations.GenerateAsync(prospect.Id, prospect.OrganizationId, default));
                Assert.Equal(1, ai.Calls);
                failed.NextAiAttemptAt = DateTime.UtcNow.AddSeconds(-1);
                await db.SaveChangesAsync();
                ai.Fail = false;
                await recommendations.GenerateAsync(prospect.Id, prospect.OrganizationId, default);
                // A new context must reuse persisted output without another provider call.
                await using var recovered = new ApplicationDbContext(options);
                var cached = await new CampaignRecommendationService(recovered, ai).GenerateAsync(prospect.Id, prospect.OrganizationId, default);
                Assert.Equal("none", cached.Service);
                Assert.Equal(2, ai.Calls);

                var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                    { ["Encryption:Key"] = "test-only-key-123456789012345678901234567890" }).Build();
                var optOut = new OreoLeads.Infrastructure.Brevo.OutreachReviewService(db,
                    new OreoLeads.Infrastructure.Security.EncryptionService(config), config);
                var payload = new OreoLeads.Infrastructure.Brevo.UnsubscribePayload("oreoleads:unsubscribe:v1", prospect.OrganizationId, prospect.LeadId, "contact@example.com");
                await optOut.UnsubscribeAsync(payload, default);
                await optOut.UnsubscribeAsync(payload, default);
                Assert.Single(await db.UnsubscribeRecords.ToListAsync());
            }
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE {name} WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}

internal sealed class RegistryFixture : ILeadSource
{
    public string ProviderName => "Fixture";
    public int PageSize => 2;
    public Task<(List<CompanySearchResultDto> Results, int TotalFound, int RawPageCount)> SearchAsync(CompanySearchRequestDto request, int page = 1, CancellationToken ct = default)
        => Task.FromResult<(List<CompanySearchResultDto>, int, int)>(([
            new() { CompanyName = "First", Siren = "111111111" },
            new() { CompanyName = "Second", Siren = "222222222" }
        ], 2, 2));
}

internal sealed class RecommendationFixture : ILeadQualificationService
{
    public int Calls { get; private set; }
    public bool Fail { get; set; } = true;
    public Task<LeadRecommendation> RecommendAsync(IReadOnlyList<OreoLeads.Application.Features.Prospecting.QualificationEvidence> evidence, CancellationToken ct)
    {
        Calls++;
        if (Fail) throw new InvalidOperationException("Temporary provider failure");
        return Task.FromResult(new LeadRecommendation("none", [], "Fixture", 12));
    }
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OREOLEADS_TEST_POSTGRES")))
            Skip = "Set OREOLEADS_TEST_POSTGRES to an isolated PostgreSQL test server (requires CREATE DATABASE).";
    }
}
