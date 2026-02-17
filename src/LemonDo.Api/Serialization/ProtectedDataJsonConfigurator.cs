namespace LemonDo.Api.Serialization;

using System.Text.Json.Serialization.Metadata;
using LemonDo.Domain.Common;
using LemonDo.Infrastructure.Security;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

/// <summary>
/// Configures JSON serialization for protected data types by assigning per-property converters
/// to <see cref="EncryptedField"/> properties (email vs display name vs sensitive note),
/// and registering converters for <see cref="ProtectedValue"/> and <see cref="RevealedField"/>.
/// </summary>
public sealed class ProtectedDataJsonConfigurator(IFieldEncryptionService encryption)
    : IConfigureOptions<JsonOptions>
{
    /// <inheritdoc />
    public void Configure(JsonOptions options)
    {
        // Global converters for types with a single converter
        options.SerializerOptions.Converters.Add(new ProtectedValueConverter());
        options.SerializerOptions.Converters.Add(new RevealedFieldConverter(encryption));

        // Per-property converters for EncryptedField — assigned via type info modifier
        // because different properties need different validators (email vs displayName vs sensitiveNote).
        // We modify the EXISTING resolver (not add a new one to the chain) so it runs on the
        // same resolver that ASP.NET Core uses for request/response deserialization.
        var emailConverter = new EncryptedEmailConverter(encryption);
        var displayNameConverter = new EncryptedDisplayNameConverter(encryption);
        var sensitiveNoteConverter = new EncryptedSensitiveNoteConverter(encryption);

        options.SerializerOptions.TypeInfoResolver = options.SerializerOptions.TypeInfoResolver?
            .WithAddedModifier(typeInfo =>
            {
                if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

                foreach (var property in typeInfo.Properties)
                {
                    if (property.PropertyType != typeof(EncryptedField)) continue;

                    property.CustomConverter = property.Name.ToLowerInvariant() switch
                    {
                        "email" => emailConverter,
                        "displayname" => displayNameConverter,
                        "sensitivenote" => sensitiveNoteConverter,
                        _ => sensitiveNoteConverter // Default to most restrictive
                    };
                }
            });
    }
}
