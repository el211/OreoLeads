namespace OreoLeads.Application.Common.Interfaces;

/// <summary>
/// Shared AES-256-GCM encryption service used by all providers (AI, Brevo, Airtable…).
/// Format: Base64(nonce[12] + tag[16] + ciphertext)
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
