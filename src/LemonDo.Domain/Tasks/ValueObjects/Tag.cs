namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>
/// Validated tag label. Must be non-empty and at most <see cref="MaxLength"/> characters.
/// Values are trimmed and normalized to lowercase on creation.
/// </summary>
public sealed class Tag : ValueObject
{
    /// <summary>Maximum allowed length for a tag: 50 characters.</summary>
    public const int MaxLength = 50;

    /// <summary>The underlying validated tag string, trimmed and normalized to lowercase.</summary>
    public string Value { get; }

    private Tag(string value)
    {
        Value = value;
    }

    // EF Core constructor
    private Tag() { Value = default!; }

    /// <summary>
    /// Creates a <see cref="Tag"/> from a string. Trims whitespace, converts to lowercase, and validates length.
    /// Returns failure if the input is null, empty, whitespace-only, or exceeds <see cref="MaxLength"/>.
    /// </summary>
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

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
