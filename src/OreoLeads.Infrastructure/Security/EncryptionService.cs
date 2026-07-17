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
        var plainBytes  = Encoding.UTF8.GetBytes(plaintext);
        var nonce       = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        var tag         = new byte[AesGcm.TagByteSizes.MaxSize];   // 16 bytes
        var ciphertext  = new byte[plainBytes.Length];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plainBytes, ciphertext, tag);

        // Layout: nonce(12) || tag(16) || ciphertext
        var combined = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce,       0, combined, 0,                             nonce.Length);
        Buffer.BlockCopy(tag,         0, combined, nonce.Length,                  tag.Length);
        Buffer.BlockCopy(ciphertext,  0, combined, nonce.Length + tag.Length,     ciphertext.Length);

        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string ciphertext)
    {
        var combined   = Convert.FromBase64String(ciphertext);
        var nonce      = combined[..12];
        var tag        = combined[12..28];
        var cipher     = combined[28..];
        var plaintext  = new byte[cipher.Length];

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Decrypt(nonce, cipher, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
