namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

public sealed class BoardTaskId : ValueObject
{
    public Guid Value { get; }

    public BoardTaskId(Guid value)
    {
        Value = value;
    }

    public static BoardTaskId New() => new(Guid.NewGuid());
    public static BoardTaskId From(Guid value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
