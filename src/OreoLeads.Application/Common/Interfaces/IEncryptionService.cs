namespace OreoLeads.Application.Common.Interfaces;

/// <summary>
/// Shared AES-256-GCM encryption service used by all providers (AI, Brevo, Airtable…).
///
/// Current format: <c>gcm:v1:&lt;Base64(nonce[12] + tag[16] + ciphertext)&gt;</c>
///
/// The versioned prefix allows format detection without attempting decryption, which
/// makes migration from legacy formats explicit and safe.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts <paramref name="plaintext"/> with AES-256-GCM and returns the result in
    /// the versioned format <c>gcm:v1:&lt;base64&gt;</c>.
    /// </summary>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts a value produced by <see cref="Encrypt"/>.
    /// Accepts both the current versioned <c>gcm:v1:&lt;base64&gt;</c> format and the
    /// legacy raw-GCM Base64 format written before versioning was introduced (AI config).
    /// Throws <see cref="System.Security.Cryptography.CryptographicException"/> on failure.
    /// </summary>
    string Decrypt(string ciphertext);

    /// <summary>
    /// Returns <c>true</c> if <paramref name="ciphertext"/> was produced by the current
    /// <see cref="Encrypt"/> implementation (i.e. starts with <c>gcm:v1:</c>).
    /// Use this to detect legacy values that require automatic migration.
    /// </summary>
    bool IsVersioned(string ciphertext);

    /// <summary>
    /// Decrypts a value in any of the three formats, never failing hard:
    /// <list type="number">
    ///   <item><c>gcm:v1:&lt;base64&gt;</c> — current versioned GCM; CBC is never attempted.</item>
    ///   <item>Raw Base64 GCM — legacy unversioned GCM (AI config before versioning).</item>
    ///   <item>Raw Base64 CBC — legacy AES-256-CBC (Brevo/Airtable before GCM migration).</item>
    /// </list>
    /// Returns <c>null</c> if all three formats fail (corrupted data or wrong key).
    /// </summary>
    string? TryDecryptWithCbcFallback(string ciphertext, string legacySecret);
}
