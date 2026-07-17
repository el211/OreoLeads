using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Tests.Security;

/// <summary>
/// Verifies that AES-256-GCM EncryptionService can still read values that were
/// previously stored using AES-256-CBC (Brevo / Airtable before the encryption migration).
///
/// The old CBC format used:
///   key  = SHA256(secret)         — 32 bytes
///   IV   = key[..16]              — 16 bytes (static, derived from key)
///   out  = Base64(CBC_ciphertext) — no nonce, no auth tag
///
/// The new GCM format is:
///   out  = Base64(nonce[12] + tag[16] + ciphertext)
/// </summary>
public class LegacyEncryptionMigrationTests
{
    private const string GcmSecret     = "TestGcmKey_AtLeast32Chars!!!!!!!";
    private const string LegacySecret  = "LegacyCbcSecret_AtLeast32Chars!!";
    private const string PlainText     = "brevo-api-key-abc123";

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static EncryptionService BuildGcmService(string secret = GcmSecret)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = secret
            })
            .Build();
        return new EncryptionService(config);
    }

    /// <summary>Produces an AES-256-CBC ciphertext identical to what the old services wrote.</summary>
    private static string EncryptWithLegacyCbc(string plaintext, string secret)
    {
        using var sha = SHA256.Create();
        var key = sha.ComputeHash(Encoding.UTF8.GetBytes(secret));
        var iv  = key[..16]; // static IV — same as old implementation

        using var aes       = Aes.Create();
        aes.Key = key;
        aes.IV  = iv;
        using var encryptor = aes.CreateEncryptor();
        var bytes           = Encoding.UTF8.GetBytes(plaintext);
        return Convert.ToBase64String(encryptor.TransformFinalBlock(bytes, 0, bytes.Length));
    }

    // ── 1. Lecture d'une ancienne valeur CBC ──────────────────────────────────

    [Fact]
    public void TryDecryptWithCbcFallback_LegacyCbcValue_ReturnsPlaintext()
    {
        var svc          = BuildGcmService();
        var legacyCipher = EncryptWithLegacyCbc(PlainText, LegacySecret);

        // The GCM attempt must fail (wrong format), then CBC fallback must succeed
        var result = svc.TryDecryptWithCbcFallback(legacyCipher, LegacySecret);

        result.Should().Be(PlainText);
    }

    [Fact]
    public void TryDecryptWithCbcFallback_NewGcmValue_ReturnsPlaintextWithoutFallback()
    {
        var svc       = BuildGcmService();
        var gcmCipher = svc.Encrypt(PlainText);

        // Should succeed via the GCM path, never reaching the CBC fallback
        var result = svc.TryDecryptWithCbcFallback(gcmCipher, LegacySecret);

        result.Should().Be(PlainText);
    }

    // ── 2. Migration vers GCM : la valeur est rechiffrée automatiquement ──────

    [Fact]
    public void AfterReadingLegacyCbc_ReEncryptedValueIsVersionedGcm()
    {
        var svc          = BuildGcmService();
        var legacyCipher = EncryptWithLegacyCbc(PlainText, LegacySecret);

        // Step 1: read the legacy CBC value (simulates GetCurrentAsync auto-migration)
        var decrypted = svc.TryDecryptWithCbcFallback(legacyCipher, LegacySecret);
        decrypted.Should().Be(PlainText);

        // Step 2: re-encrypt with GCM (produces gcm:v1: format)
        var gcmCipher = svc.Encrypt(decrypted!);

        // Step 3: the new ciphertext must have the versioned prefix
        gcmCipher.Should().StartWith("gcm:v1:", because: "Encrypt must produce the versioned format");

        // Step 4: the new ciphertext must be readable via Decrypt() — no CBC needed
        var roundTripped = svc.Decrypt(gcmCipher);
        roundTripped.Should().Be(PlainText);

        // Step 5: the versioned ciphertext must differ from the old CBC one
        gcmCipher.Should().NotBe(legacyCipher);

        // Step 6: IsVersioned correctly classifies old vs new
        svc.IsVersioned(gcmCipher).Should().BeTrue();
        svc.IsVersioned(legacyCipher).Should().BeFalse();
    }

    [Fact]
    public void VersionedValue_TryDecryptWithCbcFallback_NeverTriesCbc()
    {
        // A gcm:v1: value encrypted with key1 must return null when decrypted with key2
        // (wrong GCM key) even if legacySecret would produce a valid CBC result.
        var svc1   = BuildGcmService("KeyOne_AtLeast32Characters____!!");
        var svc2   = BuildGcmService("KeyTwo_AtLeast32Characters____!!");

        var versioned = svc1.Encrypt(PlainText); // gcm:v1: prefix
        versioned.Should().StartWith("gcm:v1:");

        // svc2 cannot GCM-decrypt it, and because it IS versioned, CBC must NOT be tried
        var result = svc2.TryDecryptWithCbcFallback(versioned, LegacySecret);
        result.Should().BeNull(because: "a gcm:v1: value must never fall back to CBC");
    }

    [Fact]
    public void MigrationIsIdempotent_VersionedValueIsNotReEncrypted()
    {
        var svc = BuildGcmService();

        // First call: encrypts and produces gcm:v1:
        var first = svc.Encrypt(PlainText);
        first.Should().StartWith("gcm:v1:");

        // IsVersioned returns true → no migration needed
        svc.IsVersioned(first).Should().BeTrue();

        // Decrypting again must return the same plaintext
        var result = svc.TryDecryptWithCbcFallback(first, LegacySecret);
        result.Should().Be(PlainText);
    }

    [Fact]
    public void GcmAndCbcProduceDifferentCiphertexts_ForSamePlaintext()
    {
        var svc          = BuildGcmService();
        var gcmCipher    = svc.Encrypt(PlainText);
        var legacyCipher = EncryptWithLegacyCbc(PlainText, GcmSecret);

        gcmCipher.Should().NotBe(legacyCipher);
    }

    // ── 3. Données corrompues ─────────────────────────────────────────────────

    [Fact]
    public void TryDecryptWithCbcFallback_CorruptedData_ReturnsNull()
    {
        var svc = BuildGcmService();

        // Garbage base64 that is neither valid GCM nor valid CBC
        const string corrupted = "dGhpcyBpcyBub3QgZW5jcnlwdGVk"; // "this is not encrypted"

        var result = svc.TryDecryptWithCbcFallback(corrupted, LegacySecret);

        // Both GCM and CBC must fail gracefully → null
        result.Should().BeNull();
    }

    [Fact]
    public void TryDecryptWithCbcFallback_TamperedGcmTag_ReturnsNull()
    {
        var svc       = BuildGcmService();
        var gcmCipher = svc.Encrypt(PlainText); // "gcm:v1:<base64>"

        // Strip prefix, tamper with the GCM authentication tag (bytes 12–27), re-attach prefix
        const string prefix = "gcm:v1:";
        var raw  = Convert.FromBase64String(gcmCipher[prefix.Length..]);
        raw[15] ^= 0xFF; // flip bits inside the tag
        var tampered = prefix + Convert.ToBase64String(raw);

        // gcm:v1: prefix → GCM path only; tag mismatch → null (no CBC fallback attempted)
        var result = svc.TryDecryptWithCbcFallback(tampered, LegacySecret);
        result.Should().BeNull();
    }

    [Fact]
    public void Decrypt_TamperedGcmTag_Throws()
    {
        var svc       = BuildGcmService();
        var gcmCipher = svc.Encrypt(PlainText); // "gcm:v1:<base64>"

        const string prefix = "gcm:v1:";
        var raw  = Convert.FromBase64String(gcmCipher[prefix.Length..]);
        raw[15] ^= 0xFF;
        var tampered = prefix + Convert.ToBase64String(raw);

        var act = () => svc.Decrypt(tampered);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_PayloadTooShort_Throws()
    {
        var svc = BuildGcmService();

        // 20-byte payload — shorter than the minimum 28 bytes (12 nonce + 16 tag)
        var tooShort = Convert.ToBase64String(new byte[20]);

        var act = () => svc.Decrypt(tooShort);
        act.Should().Throw<CryptographicException>()
           .WithMessage("*too short*");
    }

    // ── 4. Mauvaise clé ───────────────────────────────────────────────────────

    [Fact]
    public void TryDecryptWithCbcFallback_WrongLegacyKey_ReturnsNull()
    {
        var svc          = BuildGcmService();
        var legacyCipher = EncryptWithLegacyCbc(PlainText, LegacySecret);

        // Supply a completely different legacy secret → CBC padding error or garbage output
        var result = svc.TryDecryptWithCbcFallback(legacyCipher, "WrongKey_TotallyDifferent_32Chars!");

        // Should fail gracefully — wrong key produces CryptographicException or bad padding
        // Either null (on exception) or garbled text that is not the original
        if (result is not null)
            result.Should().NotBe(PlainText, because: "wrong key must not produce the original plaintext");
    }

    [Fact]
    public void Decrypt_WithWrongGcmKey_Throws()
    {
        var svc1      = BuildGcmService("KeyOne_AtLeast32Characters____!!");
        var svc2      = BuildGcmService("KeyTwo_AtLeast32Characters____!!");

        var cipher = svc1.Encrypt(PlainText);

        var act = () => svc2.Decrypt(cipher);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void TryDecryptWithCbcFallback_GcmValueWithWrongGcmKey_TriesCbcAndFails()
    {
        var svc1   = BuildGcmService("KeyOne_AtLeast32Characters____!!");
        var svc2   = BuildGcmService("KeyTwo_AtLeast32Characters____!!");
        var cipher = svc1.Encrypt(PlainText);

        // svc2 cannot decrypt with its own GCM key, and CBC fallback also fails
        var result = svc2.TryDecryptWithCbcFallback(cipher, LegacySecret);
        result.Should().BeNull();
    }
}
