namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

public sealed class TaskDescription : ValueObject
{
    public const int MaxLength = 10_000;

    public string Value { get; }

    private TaskDescription(string value)
    {
        Value = value;
    }

    public static Result<TaskDescription, DomainError> Create(string? value)
    {
        var text = value ?? string.Empty;

        if (text.Length > MaxLength)
            return Result<TaskDescription, DomainError>.Failure(
                DomainError.Validation("description", $"Task description must be {MaxLength} characters or fewer."));

        return Result<TaskDescription, DomainError>.Success(new TaskDescription(text));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
