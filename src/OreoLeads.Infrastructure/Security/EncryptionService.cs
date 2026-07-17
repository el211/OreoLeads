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
/// Current output format: <c>gcm:v1:&lt;Base64(nonce[12] + tag[16] + ciphertext)&gt;</c>
/// The 12-byte nonce is randomly generated per call — same plaintext always
/// produces a different ciphertext, preventing pattern analysis.
///
/// LEGACY FORMATS:
/// <list type="bullet">
///   <item>Raw Base64 GCM (no prefix) — written by AI config before versioning.</item>
///   <item>Raw Base64 CBC (static IV) — written by Brevo/Airtable before GCM migration.</item>
/// </list>
/// Use <see cref="TryDecryptWithCbcFallback"/> to read those old values.
/// On successful legacy decryption, callers should immediately re-encrypt via
/// <see cref="Encrypt"/> and persist the result (automatic one-time migration).
/// </summary>
internal sealed class EncryptionService : IEncryptionService
{
    private const string Prefix = "gcm:v1:";
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

    // ── Public API ────────────────────────────────────────────────────────────

    public bool IsVersioned(string ciphertext)
        => ciphertext.StartsWith(Prefix, StringComparison.Ordinal);

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

        return Prefix + Convert.ToBase64String(combined);
    }

    /// <summary>
    /// Decrypts a value from <see cref="Encrypt"/> (versioned <c>gcm:v1:</c>) or from the
    /// legacy raw-GCM format (AI config written before versioning was introduced).
    /// </summary>
    public string Decrypt(string ciphertext)
    {
        var base64 = IsVersioned(ciphertext) ? ciphertext[Prefix.Length..] : ciphertext;
        return DecryptRawGcm(base64);
    }

    /// <summary>
    /// Three-tier decryption with graceful fallback. Never throws.
    /// <list type="number">
    ///   <item>If <c>gcm:v1:</c> prefix → GCM only. CBC is never attempted.</item>
    ///   <item>No prefix → try raw GCM (legacy unversioned AI values).</item>
    ///   <item>Raw GCM failed → try AES-256-CBC with the legacy per-service secret.</item>
    /// </list>
    /// Returns <c>null</c> when all three fail.
    /// </summary>
    public string? TryDecryptWithCbcFallback(string ciphertext, string legacySecret)
    {
        // ── 1. Versioned GCM: prefix present → GCM only, no CBC ──────────────
        if (IsVersioned(ciphertext))
        {
            try { return Decrypt(ciphertext); }
            catch { return null; }
        }

        // ── 2. Legacy unversioned raw GCM ─────────────────────────────────────
        try { return DecryptRawGcm(ciphertext); }
        catch { /* fall through to CBC */ }

        // ── 3. Legacy AES-256-CBC with static IV ──────────────────────────────
        // Matches the derivation used in the old Brevo/Airtable services:
        // key = SHA256(secret), IV = key[..16]
        try
        {
            using var sha     = SHA256.Create();
            var legacyKey     = sha.ComputeHash(Encoding.UTF8.GetBytes(legacySecret));
            var legacyIv      = legacyKey[..16];

            using var aes     = Aes.Create();
            aes.Key = legacyKey;
            aes.IV  = legacyIv;

            using var decryptor = aes.CreateDecryptor();
            var bytes           = Convert.FromBase64String(ciphertext);
            var plainBytes      = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch { return null; }
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private string DecryptRawGcm(string base64)
    {
        var combined = Convert.FromBase64String(base64);

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
}
