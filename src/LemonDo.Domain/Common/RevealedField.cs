namespace LemonDo.Domain.Common;

/// <summary>
/// Holds encrypted ciphertext that will be decrypted at JSON serialization time.
/// The raw value only exists in the JSON output stream — never in a C# property.
/// Used for admin reveal and owner reveal responses.
/// </summary>
public sealed class RevealedField
{
    /// <summary>The encrypted ciphertext to be decrypted during JSON serialization.</summary>
    public string Encrypted { get; }

    /// <summary>Creates a new <see cref="RevealedField"/> from encrypted ciphertext.</summary>
    public RevealedField(string encrypted) => Encrypted = encrypted;

    /// <inheritdoc />
    public override string ToString() => "[REVEAL_PENDING]";
}
