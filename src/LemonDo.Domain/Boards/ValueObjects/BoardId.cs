namespace LemonDo.Domain.Boards.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>Strongly-typed identifier for a <see cref="Entities.Board"/> aggregate.</summary>
public sealed class BoardId : ValueObject
{
    /// <summary>The underlying GUID value.</summary>
    public Guid Value { get; }

    private BoardId(Guid value) => Value = value;

    public static BoardId New() => new(Guid.NewGuid());
    public static BoardId From(Guid value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
