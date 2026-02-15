namespace LemonDo.Domain.Boards.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>Strongly-typed identifier for a <see cref="Entities.Board"/> aggregate.</summary>
public sealed class BoardId : ValueObject
{
    /// <summary>The underlying GUID value.</summary>
    public Guid Value { get; }

    private BoardId(Guid value) => Value = value;

    /// <summary>Generates a new unique board identifier.</summary>
    public static BoardId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing GUID as a <see cref="BoardId"/>. Use when reconstructing from persistence.</summary>
    public static BoardId From(Guid value) => new(value);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
