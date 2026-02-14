namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

public sealed class ColumnName : ValueObject
{
    public const int MaxLength = 50;
    public string Value { get; }

    private ColumnName(string value) => Value = value;

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

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
