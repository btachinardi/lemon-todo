namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>
/// Validated task title. Must be non-empty and at most <see cref="MaxLength"/> characters (trimmed).
/// </summary>
public sealed class TaskTitle : ValueObject
{
    /// <summary>Maximum allowed length for a task title: 500 characters.</summary>
    public const int MaxLength = 500;

    /// <summary>The underlying validated task title string, trimmed.</summary>
    public string Value { get; }

    private TaskTitle(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a <see cref="TaskTitle"/> from a string. Trims whitespace and validates length.
    /// Returns failure if the input is null, empty, whitespace-only, or exceeds <see cref="MaxLength"/>.
    /// </summary>
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

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
