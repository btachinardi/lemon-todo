namespace LemonDo.Api.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.ValueObjects;
using LemonDo.Infrastructure.Security;

/// <summary>
/// JSON converter for email → <see cref="EncryptedField"/>: validates via <see cref="Email.Create"/>,
/// encrypts, computes email-style redaction and SHA-256 hash for lookups.
/// </summary>
public sealed class EncryptedEmailConverter(IFieldEncryptionService encryption) : JsonConverter<EncryptedField>
{
    /// <inheritdoc />
    public override EncryptedField? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (raw is null) return null;
        return Create(raw, encryption);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EncryptedField value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Redacted);
    }

    /// <summary>Creates an <see cref="EncryptedField"/> from a raw email string. Also used by test helpers.</summary>
    public static EncryptedField Create(string raw, IFieldEncryptionService encryption)
    {
        var result = Email.Create(raw);
        if (result.IsFailure)
            throw new ProtectedDataValidationException(result.Error);

        var email = result.Value;
        return new EncryptedField(
            encrypted: encryption.Encrypt(email.Value),
            redacted: email.Redacted,
            hash: ProtectedDataHasher.HashEmail(email.Value));
    }
}

/// <summary>
/// JSON converter for display name → <see cref="EncryptedField"/>: validates via <see cref="DisplayName.Create"/>,
/// encrypts, computes "J***e"-style redaction. No hash (display names don't need lookup).
/// </summary>
public sealed class EncryptedDisplayNameConverter(IFieldEncryptionService encryption) : JsonConverter<EncryptedField>
{
    /// <inheritdoc />
    public override EncryptedField? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (raw is null) return null;
        return Create(raw, encryption);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EncryptedField value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Redacted);
    }

    /// <summary>Creates an <see cref="EncryptedField"/> from a raw display name string. Also used by test helpers.</summary>
    public static EncryptedField Create(string raw, IFieldEncryptionService encryption)
    {
        var result = DisplayName.Create(raw);
        if (result.IsFailure)
            throw new ProtectedDataValidationException(result.Error);

        var displayName = result.Value;
        return new EncryptedField(
            encrypted: encryption.Encrypt(displayName.Value),
            redacted: displayName.Redacted);
    }
}

/// <summary>
/// JSON converter for sensitive note → <see cref="EncryptedField"/>: validates via <see cref="SensitiveNote.Create"/>,
/// encrypts, uses constant "[PROTECTED]" redaction. No hash.
/// </summary>
public sealed class EncryptedSensitiveNoteConverter(IFieldEncryptionService encryption) : JsonConverter<EncryptedField>
{
    /// <inheritdoc />
    public override EncryptedField? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (raw is null) return null;
        return Create(raw, encryption);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EncryptedField value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Redacted);
    }

    /// <summary>Creates an <see cref="EncryptedField"/> from a raw sensitive note string. Also used by test helpers.</summary>
    public static EncryptedField Create(string raw, IFieldEncryptionService encryption)
    {
        var result = SensitiveNote.Create(raw);
        if (result.IsFailure)
            throw new ProtectedDataValidationException(result.Error);

        return new EncryptedField(
            encrypted: encryption.Encrypt(result.Value.Value),
            redacted: SensitiveNote.RedactedValue);
    }
}
