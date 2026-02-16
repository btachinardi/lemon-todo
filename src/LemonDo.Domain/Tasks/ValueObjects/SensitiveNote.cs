namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>
/// Validated sensitive note for a task. Content is encrypted at rest and redacted by default.
/// Implements <see cref="IProtectedData"/> so the display layer knows to show "[PROTECTED]".
/// </summary>
public sealed class SensitiveNote : ValueObject<string>, IReconstructable<SensitiveNote, string>, IProtectedData
{
    /// <summary>Maximum allowed length for a sensitive note: 10,000 characters.</summary>
    public const int MaxLength = 10_000;

    /// <summary>The redacted placeholder shown in UI and logs instead of the real content.</summary>
    public const string RedactedValue = "[PROTECTED]";

    private SensitiveNote(string value) : base(value) { }

    /// <summary>
    /// Creates a <see cref="SensitiveNote"/> from a string. Trims whitespace.
    /// Returns failure if the input is empty or exceeds <see cref="MaxLength"/>.
    /// </summary>
    public static Result<SensitiveNote, DomainError> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<SensitiveNote, DomainError>.Failure(
                DomainError.Validation("sensitiveNote", "Sensitive note cannot be empty."));

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return Result<SensitiveNote, DomainError>.Failure(
                DomainError.Validation("sensitiveNote", $"Sensitive note must be {MaxLength} characters or fewer."));

        return Result<SensitiveNote, DomainError>.Success(new SensitiveNote(trimmed));
    }

    /// <inheritdoc />
    public string Redacted => RedactedValue;

    /// <summary>Reconstructs a <see cref="SensitiveNote"/> from a persistence value.</summary>
    public static SensitiveNote Reconstruct(string value) => new(value);
}
