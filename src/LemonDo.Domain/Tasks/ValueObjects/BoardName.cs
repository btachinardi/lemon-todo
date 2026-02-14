namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

public sealed class BoardName : ValueObject
{
    public const int MaxLength = 100;
    public string Value { get; }

    private BoardName(string value) => Value = value;

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

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
