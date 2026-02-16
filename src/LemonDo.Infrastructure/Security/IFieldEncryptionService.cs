namespace LemonDo.Infrastructure.Security;

/// <summary>
/// Service for encrypting and decrypting individual field values.
/// Used for PII data at rest (email, display name).
/// </summary>
public interface IFieldEncryptionService
{
    /// <summary>Encrypts a plaintext string. Returns a Base64-encoded ciphertext (IV prepended).</summary>
    string Encrypt(string plaintext);

    /// <summary>Decrypts a Base64-encoded ciphertext. Returns the original plaintext.</summary>
    string Decrypt(string ciphertext);
}
