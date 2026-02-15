namespace LemonDo.Domain.Boards.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>Strongly-typed identifier for a <see cref="Entities.Column"/>.</summary>
public sealed class ColumnId : ValueObject
{
    /// <summary>The underlying GUID value.</summary>
    public Guid Value { get; }

    public ColumnId(Guid value)
    {
        Value = value;
    }

    public static ColumnId New() => new(Guid.NewGuid());
    public static ColumnId From(Guid value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
