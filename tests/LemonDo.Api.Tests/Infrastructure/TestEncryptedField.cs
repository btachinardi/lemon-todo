namespace LemonDo.Api.Tests.Infrastructure;

using LemonDo.Api.Serialization;
using LemonDo.Domain.Common;
using LemonDo.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Test helper for creating <see cref="EncryptedField"/> and <see cref="ProtectedValue"/>
/// instances from raw strings. Uses a deterministic test encryption key.
/// </summary>
public static class TestEncryptedField
{
    private static readonly IFieldEncryptionService Encryption = CreateTestEncryption();

    /// <summary>Creates an <see cref="EncryptedField"/> from a raw email string (validates, encrypts, hashes).</summary>
    public static EncryptedField Email(string raw) => EncryptedEmailConverter.Create(raw, Encryption);

    /// <summary>Creates an <see cref="EncryptedField"/> from a raw display name string (validates, encrypts).</summary>
    public static EncryptedField DisplayName(string raw) => EncryptedDisplayNameConverter.Create(raw, Encryption);

    /// <summary>Creates an <see cref="EncryptedField"/> from a raw sensitive note string (validates, encrypts).</summary>
    public static EncryptedField SensitiveNote(string raw) => EncryptedSensitiveNoteConverter.Create(raw, Encryption);

    /// <summary>Creates a <see cref="ProtectedValue"/> wrapping a raw password string.</summary>
    public static ProtectedValue Password(string raw) => new(raw);

    private static IFieldEncryptionService CreateTestEncryption()
    {
        var key = Convert.ToBase64String(new byte[32]); // 32 zero bytes for testing
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:FieldEncryptionKey"] = key
            })
            .Build();
        return new AesFieldEncryptionService(config);
    }
}
