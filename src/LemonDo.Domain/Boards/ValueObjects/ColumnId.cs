namespace LemonDo.Domain.Boards.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>Strongly-typed identifier for a <see cref="Entities.Column"/>.</summary>
public sealed class ColumnId : ValueObject
{
    /// <summary>The underlying GUID value.</summary>
    public Guid Value { get; }

    /// <summary>Creates a <see cref="ColumnId"/> from an existing GUID. Public to support EF Core and DTO mapping.</summary>
    public ColumnId(Guid value)
    {
        Value = value;
    }

    /// <summary>Generates a new unique column identifier.</summary>
    public static ColumnId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing GUID as a <see cref="ColumnId"/>. Use when reconstructing from persistence.</summary>
    public static ColumnId From(Guid value) => new(value);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
