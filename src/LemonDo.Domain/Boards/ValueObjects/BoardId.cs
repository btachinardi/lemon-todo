namespace LemonDo.Domain.Boards.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>Strongly-typed identifier for a <see cref="Entities.Board"/> aggregate.</summary>
public sealed class BoardId : ValueObject<Guid>, IReconstructable<BoardId, Guid>
{
    private BoardId(Guid value) : base(value) { }

    /// <summary>Generates a new unique board identifier.</summary>
    public static BoardId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing GUID as a <see cref="BoardId"/>. Use when reconstructing from persistence.</summary>
    public static BoardId From(Guid value) => new(value);

    /// <summary>Reconstructs a <see cref="BoardId"/> from a persistence value.</summary>
    public static BoardId Reconstruct(Guid value) => new(value);
}
