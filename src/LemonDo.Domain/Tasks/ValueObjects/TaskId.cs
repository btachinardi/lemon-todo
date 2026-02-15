namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>
/// Strongly-typed identifier for a <see cref="Entities.Task"/> aggregate.
/// </summary>
public sealed class TaskId : ValueObject
{
    /// <summary>The underlying GUID value.</summary>
    public Guid Value { get; }

    /// <summary>Creates a <see cref="TaskId"/> from an existing GUID.</summary>
    public TaskId(Guid value)
    {
        Value = value;
    }

    /// <summary>Generates a new random <see cref="TaskId"/>.</summary>
    public static TaskId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing GUID as a <see cref="TaskId"/>.</summary>
    public static TaskId From(Guid value) => new(value);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
