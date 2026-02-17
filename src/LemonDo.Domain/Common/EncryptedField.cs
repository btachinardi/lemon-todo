namespace LemonDo.Domain.Common;

/// <summary>
/// Sealed container for encrypted protected data. The raw plaintext is never stored.
/// Created by JSON converters during deserialization: validate → encrypt → discard raw.
/// Implements <see cref="IProtectedData"/> so the Serilog destructuring policy masks it.
/// </summary>
public sealed class EncryptedField : IProtectedData
{
    /// <summary>The encrypted ciphertext (Base64). Stored in shadow properties.</summary>
    public string Encrypted { get; }

    /// <summary>A human-readable redacted form, safe for display and logging.</summary>
    public string Redacted { get; }

    /// <summary>
    /// Optional deterministic hash for database lookups (e.g., SHA-256 of email).
    /// Null for fields that don't need lookup capability.
    /// </summary>
    public string? Hash { get; }

    /// <summary>Creates a new <see cref="EncryptedField"/> with encrypted ciphertext and redacted form.</summary>
    public EncryptedField(string encrypted, string redacted, string? hash = null)
    {
        Encrypted = encrypted;
        Redacted = redacted;
        Hash = hash;
    }

    /// <inheritdoc />
    public override string ToString() => Redacted;
}
