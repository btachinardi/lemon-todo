namespace LemonDo.Domain.Boards.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>
/// Validated board name. Must be non-empty and at most <see cref="MaxLength"/> characters (trimmed).
/// </summary>
public sealed class BoardName : ValueObject
{
    /// <summary>Maximum allowed length for a board name: 100 characters.</summary>
    public const int MaxLength = 100;

    /// <summary>The underlying validated board name string.</summary>
    public string Value { get; }

    private BoardName(string value) => Value = value;

    /// <summary>
    /// Creates a <see cref="BoardName"/> from a string. Trims whitespace and validates length.
    /// Returns failure if the input is null, empty, whitespace-only, or exceeds <see cref="MaxLength"/>.
    /// </summary>
    public static Result<BoardName, DomainError> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<BoardName, DomainError>.Failure(
                new DomainError("board_name.validation", "Board name is required."));

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
            return Result<BoardName, DomainError>.Failure(
                new DomainError("board_name.validation", $"Board name must not exceed {MaxLength} characters."));

        return Result<BoardName, DomainError>.Success(new BoardName(trimmed));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
