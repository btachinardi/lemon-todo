namespace LemonDo.Api.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization;
using LemonDo.Domain.Common;

/// <summary>
/// JSON converter that reads a raw string and wraps it in a <see cref="ProtectedValue"/>.
/// On write, outputs "***" to prevent accidental serialization of the raw value.
/// </summary>
public sealed class ProtectedValueConverter : JsonConverter<ProtectedValue>
{
    /// <inheritdoc />
    public override ProtectedValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null ? null : new ProtectedValue(value);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ProtectedValue value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Redacted);
    }
}
