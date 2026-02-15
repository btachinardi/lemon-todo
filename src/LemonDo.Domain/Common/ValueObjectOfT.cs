namespace LemonDo.Domain.Common;

/// <summary>
/// Generic base class for single-value value objects. Provides <see cref="Value"/>,
/// equality via that value, and a default <see cref="ToString"/> override.
/// </summary>
/// <typeparam name="T">The type of the wrapped value (e.g., <see cref="Guid"/>, <see cref="string"/>).</typeparam>
public abstract class ValueObject<T> : ValueObject
{
    /// <summary>The underlying primitive value.</summary>
    public T Value { get; }

    /// <summary>Initializes the value object with the given primitive value.</summary>
    protected ValueObject(T value) => Value = value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value?.ToString() ?? string.Empty;
}
