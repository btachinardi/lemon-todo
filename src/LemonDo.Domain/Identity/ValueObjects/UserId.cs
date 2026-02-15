namespace LemonDo.Domain.Identity.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>
/// Strongly-typed identifier for a User.
/// </summary>
public sealed class UserId : ValueObject<Guid>, IReconstructable<UserId, Guid>
{
    /// <summary>
    /// Default user ID for single-user mode (CP1, no auth).
    /// </summary>
    public static readonly UserId Default = new(new Guid("00000000-0000-0000-0000-000000000001"));

    /// <summary>Creates a <see cref="UserId"/> from an existing GUID.</summary>
    public UserId(Guid value) : base(value) { }

    /// <summary>Generates a new random <see cref="UserId"/>.</summary>
    public static UserId New() => new(Guid.NewGuid());

    /// <summary>Reconstructs a <see cref="UserId"/> from a persistence value.</summary>
    public static UserId Reconstruct(Guid value) => new(value);
}
