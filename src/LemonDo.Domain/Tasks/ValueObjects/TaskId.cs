namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

public sealed class TaskId : ValueObject
{
    public Guid Value { get; }

    public TaskId(Guid value)
    {
        Value = value;
    }

    public static TaskId New() => new(Guid.NewGuid());
    public static TaskId From(Guid value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
