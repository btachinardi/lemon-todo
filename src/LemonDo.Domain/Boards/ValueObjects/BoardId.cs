namespace LemonDo.Domain.Boards.ValueObjects;

using LemonDo.Domain.Common;

public sealed class BoardId : ValueObject
{
    public Guid Value { get; }

    private BoardId(Guid value) => Value = value;

    public static BoardId New() => new(Guid.NewGuid());
    public static BoardId From(Guid value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
