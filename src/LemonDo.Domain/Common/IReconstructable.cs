namespace LemonDo.Domain.Common;

/// <summary>
/// Standardizes reconstruction of value objects from trusted raw values.
/// Used by EF Core conversions and serialization — bypasses domain validation.
/// </summary>
/// <typeparam name="TSelf">The concrete value object type being reconstructed.</typeparam>
/// <typeparam name="TValue">The primitive type stored in persistence (e.g., <see cref="Guid"/>, <see cref="string"/>).</typeparam>
public interface IReconstructable<TSelf, TValue> where TSelf : ValueObject<TValue>
{
    /// <summary>
    /// Reconstructs a value object from a trusted persistence value, bypassing validation.
    /// </summary>
    static abstract TSelf Reconstruct(TValue value);
}
