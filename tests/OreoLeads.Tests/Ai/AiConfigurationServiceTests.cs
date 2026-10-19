using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Ai;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Tests.Ai;

public class AiConfigurationServiceTests
{
    [Fact]
    public async Task Save_RemainsVisibleAcrossRequests_AndIsIsolatedByOrganization()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var tenant = new TenantContext();
        var organizationId = Guid.NewGuid();
        tenant.SetOrganization(organizationId);
        var encryption = new EncryptionService(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "TestEncryptionKey_AtLeast32Chars!!"
            }).Build());

        // An old unowned configuration must not be claimed by an arbitrary tenant.
        await using (var seed = new ApplicationDbContext(options))
        {
            seed.AiConfigurations.Add(new AiConfiguration { IsEnabled = true, Model = "legacy" });
            await seed.SaveChangesAsync();
        }

        Guid savedId;
        await using (var db = new ApplicationDbContext(options, tenant))
        {
            var saved = await new AiConfigurationService(db, encryption).SaveAsync(new()
            {
                ProviderType = AiProviderType.OpenAI,
                Model = "gpt-6-luna",
                ApiKey = "test-api-key",
                IsEnabled = true
            });
            savedId = saved.Id;
            Assert.Equal(organizationId, saved.OrganizationId);
        }

        await using (var db = new ApplicationDbContext(options, tenant))
        {
            var service = new AiConfigurationService(db, encryption);
            var loaded = await service.GetCurrentAsync();
            Assert.NotNull(loaded);
            Assert.True(loaded.IsEnabled);
            Assert.Equal("gpt-6-luna", loaded.Model);
            Assert.Equal("test-api-key", service.GetDecryptedApiKey(loaded));

            var updated = await service.SaveAsync(new UpdateAiConfigurationDto
            {
                ProviderType = AiProviderType.OpenAI, Model = "gpt-6-sol", IsEnabled = true
            });
            Assert.Equal(savedId, updated.Id);
            Assert.Equal("test-api-key", service.GetDecryptedApiKey(updated));
            Assert.Equal(1, await db.AiConfigurations.CountAsync());
        }

        var otherTenant = new TenantContext();
        otherTenant.SetOrganization(Guid.NewGuid());
        await using var otherDb = new ApplicationDbContext(options, otherTenant);
        Assert.Null(await new AiConfigurationService(otherDb, encryption).GetCurrentAsync());
    }
}
