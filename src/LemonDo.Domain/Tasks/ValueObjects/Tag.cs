namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>
/// Validated tag label. Must be non-empty and at most <see cref="MaxLength"/> characters.
/// Values are trimmed and normalized to lowercase on creation.
/// </summary>
public sealed class Tag : ValueObject<string>, IReconstructable<Tag, string>
{
    /// <summary>Maximum allowed length for a tag: 50 characters.</summary>
    public const int MaxLength = 50;

    private Tag(string value) : base(value) { }

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

        if (normalized.All(c => char.GetUnicodeCategory(c) is
                System.Globalization.UnicodeCategory.Format or
                System.Globalization.UnicodeCategory.Control or
                System.Globalization.UnicodeCategory.OtherNotAssigned or
                System.Globalization.UnicodeCategory.Surrogate))
            return Result<Tag, DomainError>.Failure(
                DomainError.Validation("tag", "Tag must contain visible characters."));

        return Result<Tag, DomainError>.Success(new Tag(normalized));
    }

    /// <summary>Reconstructs a <see cref="Tag"/> from a persistence value.</summary>
    public static Tag Reconstruct(string value) => new(value);
}
