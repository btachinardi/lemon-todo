namespace LemonDo.Domain.Boards.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>Strongly-typed identifier for a <see cref="Entities.Column"/>.</summary>
public sealed class ColumnId : ValueObject<Guid>, IReconstructable<ColumnId, Guid>
{
    /// <summary>Creates a <see cref="ColumnId"/> from an existing GUID. Public to support EF Core and DTO mapping.</summary>
    public ColumnId(Guid value) : base(value) { }

    /// <summary>Generates a new unique column identifier.</summary>
    public static ColumnId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing GUID as a <see cref="ColumnId"/>. Use when reconstructing from persistence.</summary>
    public static ColumnId From(Guid value) => new(value);

    /// <summary>Reconstructs a <see cref="ColumnId"/> from a persistence value.</summary>
    public static ColumnId Reconstruct(Guid value) => new(value);
}
