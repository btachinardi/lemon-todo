namespace LemonDo.Domain.Administration.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>Strongly-typed identifier for an <see cref="Entities.AuditEntry"/>.</summary>
public sealed class AuditEntryId : ValueObject<Guid>, IReconstructable<AuditEntryId, Guid>
{
    /// <summary>Creates an <see cref="AuditEntryId"/> from an existing GUID.</summary>
    public AuditEntryId(Guid value) : base(value) { }

    /// <summary>Generates a new random <see cref="AuditEntryId"/>.</summary>
    public static AuditEntryId New() => new(Guid.NewGuid());

    /// <summary>Reconstructs an <see cref="AuditEntryId"/> from a persistence value.</summary>
    public static AuditEntryId Reconstruct(Guid value) => new(value);
}
