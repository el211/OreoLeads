using FluentAssertions;
using Microsoft.Extensions.Configuration;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Tests.Security;

/// <summary>
/// Tests for the shared AES-256-GCM EncryptionService.
/// All tests are self-contained — no DB access needed.
/// </summary>
public class EncryptionTests
{
    private readonly EncryptionService _svc;

    public EncryptionTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "TestEncryptionKey_AtLeast32Chars!!"
            })
            .Build();

        _svc = new EncryptionService(config);
    }

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginal()
    {
        const string original = "sk-test-api-key-12345";
        var encrypted = _svc.Encrypt(original);
        var decrypted = _svc.Decrypt(encrypted);
        decrypted.Should().Be(original);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertexts_ForSamePlaintext()
    {
        // AES-GCM with random nonce — same plaintext never produces same ciphertext
        const string plain = "same-key";
        var c1 = _svc.Encrypt(plain);
        var c2 = _svc.Encrypt(plain);
        c1.Should().NotBe(c2, because: "random nonce must produce different ciphertext each time");
    }

    [Fact]
    public void Decrypt_WithTamperedCiphertext_Throws()
    {
        const string plain = "sk-api-key-secret";
        var encrypted = _svc.Encrypt(plain); // "gcm:v1:<base64>"

        // Strip the prefix, tamper with the last byte, re-attach prefix
        const string prefix = "gcm:v1:";
        var bytes = Convert.FromBase64String(encrypted[prefix.Length..]);
        bytes[^1] ^= 0xFF;
        var tampered = prefix + Convert.ToBase64String(bytes);

        var act = () => _svc.Decrypt(tampered);
        act.Should().Throw<Exception>(because: "GCM authentication tag verification must fail");
    }

    [Fact]
    public void Encrypt_ProducesVersionedPrefix()
    {
        var encrypted = _svc.Encrypt("anything");
        encrypted.Should().StartWith("gcm:v1:", because: "all new values must use the versioned format");
    }

    [Fact]
    public void IsVersioned_VersionedValue_ReturnsTrue()
    {
        var encrypted = _svc.Encrypt("test");
        _svc.IsVersioned(encrypted).Should().BeTrue();
    }

    [Fact]
    public void IsVersioned_RawBase64_ReturnsFalse()
    {
        _svc.IsVersioned(Convert.ToBase64String(new byte[32])).Should().BeFalse();
    }

    [Fact]
    public void Encrypt_EmptyString_RoundTrips()
    {
        var encrypted = _svc.Encrypt(string.Empty);
        var decrypted = _svc.Decrypt(encrypted);
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void Encrypt_LongApiKey_RoundTrips()
    {
        var longKey   = new string('x', 512);
        var encrypted = _svc.Encrypt(longKey);
        var decrypted = _svc.Decrypt(encrypted);
        decrypted.Should().Be(longKey);
    }

    [Fact]
    public void FallbackKey_Ai_EncryptionKey_IsUsedWhenEncryptionKeyAbsent()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:EncryptionKey"] = "FallbackAiKey_AtLeast32Chars!!!"
            })
            .Build();

        var svc       = new EncryptionService(config);
        const string plain = "brevo-api-key";
        var encrypted = svc.Encrypt(plain);
        svc.Decrypt(encrypted).Should().Be(plain);
    }

    [Fact]
    public void DifferentKeys_ProduceDifferentCiphertexts()
    {
        var cfg1 = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Encryption:Key"] = "KeyOne_AtLeast32Characters____!!" })
            .Build();
        var cfg2 = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Encryption:Key"] = "KeyTwo_AtLeast32Characters____!!" })
            .Build();

        var svc1 = new EncryptionService(cfg1);
        var svc2 = new EncryptionService(cfg2);

        const string plain = "secret";
        var enc1 = svc1.Encrypt(plain);

        // svc2 must not be able to decrypt what svc1 produced
        var act = () => svc2.Decrypt(enc1);
        act.Should().Throw<Exception>();
    }
}
