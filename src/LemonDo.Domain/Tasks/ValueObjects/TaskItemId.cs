namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

public sealed class TaskItemId : ValueObject
{
    public Guid Value { get; }

    public TaskItemId(Guid value)
    {
        Value = value;
    }

    public static TaskItemId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
