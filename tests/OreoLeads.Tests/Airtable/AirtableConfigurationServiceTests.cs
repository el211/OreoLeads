using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OreoLeads.Application.Features.Airtable.DTOs;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Airtable;
using OreoLeads.Infrastructure.Identity;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Tests.Airtable;

public class AirtableConfigurationServiceTests
{
    // ── 1. EncryptDecrypt_Token_RoundTrips ────────────────────────────────────
    // The encryption is now handled by the shared EncryptionService (AES-256-GCM).
    // We verify the round-trip via GetDecryptedAccessToken after a Save.

    [Fact]
    public async Task EncryptDecrypt_Token_RoundTrips()
    {
        var svc   = BuildService();
        const string original = "pat_super_secret_token_123";

        var dto = new UpdateAirtableConfigurationDto(
            AccessToken:      original,
            ConnectionName:   "RoundTrip",
            BaseId:           "appTest",
            TableIdOrName:    "T1",
            IsEnabled:        false,
            SyncDirection:    SyncDirection.OreoLeadsToAirtable,
            ConflictStrategy: ConflictStrategy.OreoLeadsWins
        );
        var config    = await svc.SaveAsync(dto, null);
        var decrypted = svc.GetDecryptedAccessToken(config);

        decrypted.Should().Be(original);
        config.EncryptedAccessToken.Should().NotBe(original);
    }

    // ── 2. GetCurrentAsync_NoConfig_ReturnsNull ───────────────────────────────

    [Fact]
    public async Task GetCurrentAsync_NoConfig_ReturnsNull()
    {
        var svc    = BuildService();
        var result = await svc.GetCurrentAsync(null);
        result.Should().BeNull();
    }

    // ── 3. SaveAsync_NewConfig_EncryptsToken ──────────────────────────────────

    [Fact]
    public async Task SaveAsync_NewConfig_EncryptsToken()
    {
        var svc = BuildService();
        var dto = new UpdateAirtableConfigurationDto(
            AccessToken:      "pat_my_access_token",
            ConnectionName:   "Test Config",
            BaseId:           "appBase123",
            TableIdOrName:    "Leads",
            IsEnabled:        true,
            SyncDirection:    SyncDirection.OreoLeadsToAirtable,
            ConflictStrategy: ConflictStrategy.OreoLeadsWins
        );

        var config = await svc.SaveAsync(dto, null);

        config.Should().NotBeNull();
        config.ConnectionName.Should().Be("Test Config");
        config.EncryptedAccessToken.Should().NotBeNullOrWhiteSpace();
        config.EncryptedAccessToken.Should().NotBe("pat_my_access_token");
        config.IsEnabled.Should().BeTrue();
    }

    // ── 4. SaveAsync_ExistingConfig_UpdatesWithoutClearingToken ──────────────

    [Fact]
    public async Task SaveAsync_ExistingConfig_UpdatesWithoutClearingToken()
    {
        var svc = BuildService();

        // Create initial config with token
        var dto1 = new UpdateAirtableConfigurationDto(
            AccessToken:      "pat_original_token",
            ConnectionName:   "Initial",
            BaseId:           "appBase123",
            TableIdOrName:    "Leads",
            IsEnabled:        false,
            SyncDirection:    SyncDirection.OreoLeadsToAirtable,
            ConflictStrategy: ConflictStrategy.OreoLeadsWins
        );
        var first = await svc.SaveAsync(dto1, null);
        var encryptedToken = first.EncryptedAccessToken;

        // Update without providing a token
        var dto2 = new UpdateAirtableConfigurationDto(
            AccessToken:      null,
            ConnectionName:   "Updated",
            BaseId:           "appBase456",
            TableIdOrName:    "Contacts",
            IsEnabled:        true,
            SyncDirection:    SyncDirection.Bidirectional,
            ConflictStrategy: ConflictStrategy.AirtableWins
        );
        var second = await svc.SaveAsync(dto2, null);

        // Token should be preserved
        second.EncryptedAccessToken.Should().Be(encryptedToken);
        second.ConnectionName.Should().Be("Updated");
        second.IsEnabled.Should().BeTrue();
    }

    // ── 5. TestConnectionAsync_NoConfig_ReturnsFailed ─────────────────────────

    [Fact]
    public async Task TestConnectionAsync_NoConfig_ReturnsFailed()
    {
        var svc    = BuildService();
        var result = await svc.TestConnectionAsync(null);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not configured");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AirtableConfigurationService BuildService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new ApplicationDbContext(options, new TenantContext());

        var airtable = new StubAirtableService();

        var encConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "TestEncryptionKey_AtLeast32Chars!!"
            })
            .Build();
        var encryption = new EncryptionService(encConfig);

        return new AirtableConfigurationService(db, airtable, encryption, encConfig);
    }
}

