namespace LemonDo.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

/// <summary>
/// AES-256-GCM field encryption service. Each encryption operation generates a random
/// 12-byte IV (nonce) and 16-byte authentication tag. The output format is:
/// <c>Base64(IV[12] + Tag[16] + Ciphertext[N])</c>.
/// </summary>
public sealed class AesFieldEncryptionService : IFieldEncryptionService
{
    private const int IvLength = 12;
    private const int TagLength = 16;

    private readonly byte[] _key;

    /// <summary>Creates the service with the encryption key from configuration.</summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when Encryption:FieldEncryptionKey is not configured or is shorter than 32 bytes.
    /// </exception>
    public AesFieldEncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Encryption:FieldEncryptionKey"]
            ?? throw new InvalidOperationException(
                "Encryption:FieldEncryptionKey is not configured. " +
                "Generate a 32-byte key: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))");

        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption key must be exactly 32 bytes (256 bits).");
    }

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var iv = RandomNumberGenerator.GetBytes(IvLength);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagLength];

        using var aes = new AesGcm(_key, TagLength);
        aes.Encrypt(iv, plaintextBytes, ciphertext, tag);

        // Output: IV + Tag + Ciphertext
        var result = new byte[IvLength + TagLength + ciphertext.Length];
        Buffer.BlockCopy(iv, 0, result, 0, IvLength);
        Buffer.BlockCopy(tag, 0, result, IvLength, TagLength);
        Buffer.BlockCopy(ciphertext, 0, result, IvLength + TagLength, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    /// <inheritdoc />
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown when the ciphertext is corrupted or cannot be authenticated.
    /// </exception>
    public string Decrypt(string ciphertextBase64)
    {
        var combined = Convert.FromBase64String(ciphertextBase64);

        if (combined.Length < IvLength + TagLength)
            throw new CryptographicException("Invalid ciphertext: too short.");

        var iv = combined.AsSpan(0, IvLength);
        var tag = combined.AsSpan(IvLength, TagLength);
        var ciphertext = combined.AsSpan(IvLength + TagLength);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagLength);
        aes.Decrypt(iv, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
