namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>
/// Validated task description. Allows empty strings; rejects values exceeding <see cref="MaxLength"/> characters.
/// </summary>
public sealed class TaskDescription : ValueObject
{
    /// <summary>Maximum allowed length for a task description: 10,000 characters.</summary>
    public const int MaxLength = 10_000;

    /// <summary>The underlying task description string. Can be empty but never null.</summary>
    public string Value { get; }

    private TaskDescription(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a <see cref="TaskDescription"/> from a string. Accepts empty strings and treats null as empty.
    /// Returns failure if the input exceeds <see cref="MaxLength"/>.
    /// </summary>
    public static Result<TaskDescription, DomainError> Create(string? value)
    {
        var text = value ?? string.Empty;

        if (text.Length > MaxLength)
            return Result<TaskDescription, DomainError>.Failure(
                DomainError.Validation("description", $"Task description must be {MaxLength} characters or fewer."));

        return Result<TaskDescription, DomainError>.Success(new TaskDescription(text));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
