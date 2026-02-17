namespace LemonDo.Domain.Common;

/// <summary>
/// Wraps a raw sensitive value (e.g., a password) to prevent accidental logging.
/// Not encrypted — the consuming service needs raw access.
/// Implements <see cref="IProtectedData"/> so the Serilog destructuring policy masks it.
/// </summary>
public sealed class ProtectedValue : IProtectedData
{
    /// <summary>The raw value. Only consumed by services that need it (e.g., password verification).</summary>
    public string Value { get; }

    /// <inheritdoc />
    public string Redacted => "***";

    /// <summary>Creates a new <see cref="ProtectedValue"/> wrapping the given raw value.</summary>
    public ProtectedValue(string value) => Value = value;

    /// <inheritdoc />
    public override string ToString() => "***";
}
