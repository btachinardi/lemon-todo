namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

public sealed class Tag : ValueObject
{
    public const int MaxLength = 50;

    public string Value { get; }

    private Tag(string value)
    {
        Value = value;
    }

    public static Result<Tag, DomainError> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Tag, DomainError>.Failure(
                DomainError.Validation("tag", "Tag is required."));

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
            return Result<Tag, DomainError>.Failure(
                DomainError.Validation("tag", $"Tag must be {MaxLength} characters or fewer."));

        return Result<Tag, DomainError>.Success(new Tag(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
