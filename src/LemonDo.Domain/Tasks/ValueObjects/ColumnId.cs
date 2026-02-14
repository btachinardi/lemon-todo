namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

public sealed class ColumnId : ValueObject
{
    public Guid Value { get; }

    public ColumnId(Guid value)
    {
        Value = value;
    }

    public static ColumnId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
