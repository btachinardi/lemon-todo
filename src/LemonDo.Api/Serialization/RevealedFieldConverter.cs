namespace LemonDo.Api.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization;
using LemonDo.Domain.Common;
using LemonDo.Infrastructure.Security;

/// <summary>
/// JSON converter that decrypts a <see cref="RevealedField"/>'s ciphertext during serialization.
/// The raw plaintext only exists in the JSON output stream — never in a C# property.
/// Read is not supported (RevealedField is output-only).
/// </summary>
public sealed class RevealedFieldConverter(IFieldEncryptionService encryption) : JsonConverter<RevealedField>
{
    /// <inheritdoc />
    public override RevealedField? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("RevealedField is output-only and cannot be deserialized.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, RevealedField value, JsonSerializerOptions options)
    {
        var plaintext = encryption.Decrypt(value.Encrypted);
        writer.WriteStringValue(plaintext);
    }
}
