namespace OreoLeads.Application.Common.Interfaces;

/// <summary>
/// Shared AES-256-GCM encryption service used by all providers (AI, Brevo, Airtable…).
/// Format: Base64(nonce[12] + tag[16] + ciphertext)
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);

    /// <summary>
    /// Attempts GCM decryption first. If that fails (wrong format, wrong key or legacy CBC
    /// value), falls back to AES-256-CBC decryption using the provided legacy secret.
    /// Returns null if both attempts fail (e.g. data is corrupted or uses a different key).
    ///
    /// Use this in services that previously persisted values with AES-256-CBC so that
    /// existing database rows are still readable after the migration to GCM.
    /// The value is silently re-encrypted to GCM on the next SaveAsync call.
    /// </summary>
    string? TryDecryptWithCbcFallback(string ciphertext, string legacySecret);
}
