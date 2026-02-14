namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>
/// Validated task title. Must be non-empty and at most <see cref="MaxLength"/> characters (trimmed).
/// </summary>
public sealed class TaskTitle : ValueObject
{
    public const int MaxLength = 500;

    public string Value { get; }

    private TaskTitle(string value)
    {
        Value = value;
    }

    public static Result<TaskTitle, DomainError> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<TaskTitle, DomainError>.Failure(
                DomainError.Validation("title", "Task title is required."));

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return Result<TaskTitle, DomainError>.Failure(
                DomainError.Validation("title", $"Task title must be {MaxLength} characters or fewer."));

        return Result<TaskTitle, DomainError>.Success(new TaskTitle(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
