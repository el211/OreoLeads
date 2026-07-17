using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Infrastructure.Security;

/// <summary>
/// Single AES-256-GCM encryption implementation shared across all providers.
/// Uses configuration key "Encryption:Key" (falls back to "Ai:EncryptionKey" for
/// backward compatibility, then to the built-in default).
///
/// Format: Base64(nonce[12] + tag[16] + ciphertext)
/// The 12-byte nonce is randomly generated per encryption — same plaintext always
/// produces a different ciphertext, preventing pattern analysis.
///
/// LEGACY NOTE: Brevo and Airtable previously used AES-256-CBC with a static IV.
/// Use TryDecryptWithCbcFallback() to read those old values during the transition period.
/// Values are automatically re-encrypted to GCM on the next SaveAsync call.
/// </summary>
internal sealed class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        var secret =
            configuration["Encryption:Key"]
            ?? configuration["Ai:EncryptionKey"]
            ?? "OreoLeadsDefaultEncryptionKey32!";

        using var sha = SHA256.Create();
        _key = sha.ComputeHash(Encoding.UTF8.GetBytes(secret)); // 32 bytes
    }

    public string Encrypt(string plaintext)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce      = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        var tag        = new byte[AesGcm.TagByteSizes.MaxSize];   // 16 bytes
        var ciphertext = new byte[plainBytes.Length];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plainBytes, ciphertext, tag);

        // Layout: nonce(12) || tag(16) || ciphertext
        var combined = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce,      0, combined, 0,                         nonce.Length);
        Buffer.BlockCopy(tag,        0, combined, nonce.Length,               tag.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length + tag.Length,  ciphertext.Length);

        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string ciphertext)
    {
        var combined  = Convert.FromBase64String(ciphertext);

        // A valid GCM payload must be at least 28 bytes (12 nonce + 16 tag + 0 plaintext).
        if (combined.Length < 28)
            throw new CryptographicException(
                "Ciphertext too short to be a valid AES-256-GCM payload (minimum 28 bytes).");

        var nonce     = combined[..12];
        var tag       = combined[12..28];
        var cipher    = combined[28..];
        var plaintext = new byte[cipher.Length];

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Decrypt(nonce, cipher, tag, plaintext); // throws CryptographicException on tag mismatch

        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>
    /// Tries GCM first, then falls back to AES-256-CBC using the legacy secret.
    ///
    /// Detection works reliably because GCM includes a 16-byte authentication tag that
    /// cryptographically binds the key to the ciphertext. Any value not produced by
    /// this key under GCM will cause tag verification to fail (CryptographicException),
    /// which serves as the signal to try the legacy path.
    ///
    /// After successful CBC decryption the caller should re-save the value so it is
    /// re-encrypted with GCM on the next write (transparent one-time migration).
    /// </summary>
    public string? TryDecryptWithCbcFallback(string ciphertext, string legacySecret)
    {
        // ── 1. Try GCM (new format) ──────────────────────────────────────────
        try { return Decrypt(ciphertext); }
        catch { /* not GCM or wrong key — fall through */ }

        // ── 2. Fallback: AES-256-CBC with legacy static IV ───────────────────
        // This matches the derivation used in the old BrevoConfigurationService
        // and AirtableConfigurationService (SHA256(secret), IV = key[..16]).
        try
        {
            using var sha = SHA256.Create();
            var legacyKey = sha.ComputeHash(Encoding.UTF8.GetBytes(legacySecret));
            var legacyIv  = legacyKey[..16];

            using var aes       = Aes.Create();
            aes.Key = legacyKey;
            aes.IV  = legacyIv;

            using var decryptor = aes.CreateDecryptor();
            var bytes           = Convert.FromBase64String(ciphertext);
            var plainBytes      = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch { return null; }
    }
}
