using FluentAssertions;
using Microsoft.Extensions.Configuration;
using OreoLeads.Infrastructure.Ai;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Security;

/// <summary>
/// Tests for AES-GCM encryption used to store API keys.
/// All tests are self-contained — no DB access needed.
/// </summary>
public class EncryptionTests
{
    private readonly AiConfigurationService _svc;

    public EncryptionTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:EncryptionKey"] = "TestEncryptionKey_AtLeast32Chars!!"
            })
            .Build();

        // Pass a null DbContext — tests only call Encrypt/Decrypt
        _svc = new AiConfigurationService(null!, config);
    }

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginal()
    {
        const string original = "sk-test-api-key-12345";
        var encrypted = _svc.EncryptApiKey(original);
        var decrypted = _svc.DecryptApiKey(encrypted);
        decrypted.Should().Be(original);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertexts_ForSamePlaintext()
    {
        // AES-GCM with random nonce — same plaintext never produces same ciphertext
        const string plain = "same-key";
        var c1 = _svc.EncryptApiKey(plain);
        var c2 = _svc.EncryptApiKey(plain);
        c1.Should().NotBe(c2, because: "random nonce must produce different ciphertext each time");
    }

    [Fact]
    public void Decrypt_WithTamperedCiphertext_Throws()
    {
        const string plain = "sk-api-key-secret";
        var encrypted = _svc.EncryptApiKey(plain);

        // Tamper with the last byte of the Base64-decoded payload
        var bytes = Convert.FromBase64String(encrypted);
        bytes[^1] ^= 0xFF;
        var tampered = Convert.ToBase64String(bytes);

        var act = () => _svc.DecryptApiKey(tampered);
        act.Should().Throw<Exception>(because: "GCM authentication tag verification must fail");
    }

    [Fact]
    public void Encrypt_EmptyString_RoundTrips()
    {
        var encrypted = _svc.EncryptApiKey(string.Empty);
        var decrypted = _svc.DecryptApiKey(encrypted);
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void Encrypt_LongApiKey_RoundTrips()
    {
        var longKey = new string('x', 512);
        var encrypted = _svc.EncryptApiKey(longKey);
        var decrypted = _svc.DecryptApiKey(encrypted);
        decrypted.Should().Be(longKey);
    }

    [Fact]
    public void GetDecryptedApiKey_WithNullEncryptedKey_ReturnsNull()
    {
        var config = new OreoLeads.Domain.Entities.AiConfiguration { EncryptedApiKey = null };
        _svc.GetDecryptedApiKey(config).Should().BeNull();
    }
}
