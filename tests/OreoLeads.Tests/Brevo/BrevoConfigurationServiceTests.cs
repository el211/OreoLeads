using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Brevo;
using OreoLeads.Infrastructure.Identity;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Tests.Brevo;

public class BrevoConfigurationServiceTests
{
    // ── 1. GetCurrentAsync_LegacyCbcValue_MigratesOnRead ─────────────────────

    [Fact]
    public async Task GetCurrentAsync_LegacyCbcValue_MigratesOnRead()
    {
        var (svc, db) = BuildService();
        const string legacyCbcSecret = "OreoLeadsBrevoDefaultSecretKey32!";

        // Seed a row with a legacy CBC-encrypted API key
        var legacyCipher = EncryptWithLegacyCbc("brevo-api-key-secret", legacyCbcSecret);
        legacyCipher.Should().NotStartWith("gcm:v1:");

        db.BrevoConfigurations.Add(new BrevoConfiguration
        {
            SenderName    = "Test",
            SenderEmail   = "test@example.com",
            EncryptedApiKey = legacyCipher,
        });
        await db.SaveChangesAsync();

        // First read — auto-migration must fire
        var config = await svc.GetCurrentAsync();

        config!.EncryptedApiKey.Should().StartWith("gcm:v1:", because: "auto-migration must produce versioned format");
        svc.GetDecryptedApiKey(config).Should().Be("brevo-api-key-secret");
    }

    // ── 2. GetCurrentAsync_AlreadyVersioned_IsIdempotent ─────────────────────

    [Fact]
    public async Task GetCurrentAsync_AlreadyVersioned_IsIdempotent()
    {
        var (svc, db) = BuildService();
        var encryption = BuildEncryption();

        // Seed a row with a versioned (current) value
        var versioned = encryption.Encrypt("brevo-api-key-current");
        versioned.Should().StartWith("gcm:v1:");

        db.BrevoConfigurations.Add(new BrevoConfiguration
        {
            SenderName      = "Test",
            SenderEmail     = "test@example.com",
            EncryptedApiKey = versioned,
        });
        await db.SaveChangesAsync();

        var before = (await db.BrevoConfigurations.FirstAsync()).UpdatedAt;

        // First read — no migration should be triggered
        var config = await svc.GetCurrentAsync();
        config!.EncryptedApiKey.Should().Be(versioned, because: "versioned value must not be re-encrypted");

        var after = (await db.BrevoConfigurations.FirstAsync()).UpdatedAt;
        after.Should().Be(before, because: "no DB write should occur when already versioned");
    }

    // ── 3. GetCurrentAsync_LegacyUnversionedGcmValue_MigratesOnRead ──────────

    [Fact]
    public async Task GetCurrentAsync_LegacyUnversionedGcmValue_MigratesOnRead()
    {
        var (svc, db) = BuildService();
        var encryption = BuildEncryption();

        // Seed a legacy raw GCM value (no prefix)
        var rawGcm = encryption.Encrypt("brevo-legacy-gcm")["gcm:v1:".Length..];
        rawGcm.Should().NotStartWith("gcm:v1:");

        db.BrevoConfigurations.Add(new BrevoConfiguration
        {
            SenderName      = "Test",
            SenderEmail     = "test@example.com",
            EncryptedApiKey = rawGcm,
        });
        await db.SaveChangesAsync();

        var config = await svc.GetCurrentAsync();

        config!.EncryptedApiKey.Should().StartWith("gcm:v1:");
        svc.GetDecryptedApiKey(config).Should().Be("brevo-legacy-gcm");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (BrevoConfigurationService svc, ApplicationDbContext db) BuildService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db        = new ApplicationDbContext(options, new TenantContext());
        var encConfig = BuildEncConfig();
        var encryption = new EncryptionService(encConfig);

        var svc = new BrevoConfigurationService(db, new StubBrevoService(), encryption, encConfig);
        return (svc, db);
    }

    private static EncryptionService BuildEncryption()
        => new(BuildEncConfig());

    private static IConfiguration BuildEncConfig()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "TestEncryptionKey_AtLeast32Chars!!"
            })
            .Build();

    private static string EncryptWithLegacyCbc(string plaintext, string secret)
    {
        using var sha = SHA256.Create();
        var key = sha.ComputeHash(Encoding.UTF8.GetBytes(secret));
        var iv  = key[..16];

        using var aes       = Aes.Create();
        aes.Key = key;
        aes.IV  = iv;
        using var encryptor = aes.CreateEncryptor();
        var bytes           = Encoding.UTF8.GetBytes(plaintext);
        return Convert.ToBase64String(encryptor.TransformFinalBlock(bytes, 0, bytes.Length));
    }
}
