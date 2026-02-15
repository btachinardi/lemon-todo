namespace LemonDo.Domain.Boards.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>
/// Validated column name. Must be non-empty and at most <see cref="MaxLength"/> characters (trimmed).
/// </summary>
public sealed class ColumnName : ValueObject
{
    /// <summary>Maximum allowed length for a column name: 50 characters.</summary>
    public const int MaxLength = 50;

    /// <summary>The underlying validated column name string.</summary>
    public string Value { get; }

    private ColumnName(string value) => Value = value;

    /// <summary>
    /// Creates a <see cref="ColumnName"/> from a string. Trims whitespace and validates length.
    /// Returns failure if the input is null, empty, whitespace-only, or exceeds <see cref="MaxLength"/>.
    /// </summary>
    public static Result<ColumnName, DomainError> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<ColumnName, DomainError>.Failure(
                new DomainError("column_name.validation", "Column name is required."));

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
            return Result<ColumnName, DomainError>.Failure(
                new DomainError("column_name.validation", $"Column name must not exceed {MaxLength} characters."));

        return Result<ColumnName, DomainError>.Success(new ColumnName(trimmed));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
