namespace LemonDo.Infrastructure.Persistence.Extensions;

using LemonDo.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core <see cref="PropertyBuilder{TProperty}"/> extensions that wire up
/// <see cref="ValueObject{T}"/> conversions via <see cref="IReconstructable{TSelf,TValue}"/>.
/// </summary>
public static class ValueObjectPropertyExtensions
{
    /// <summary>
    /// Configures a <see cref="Guid"/>-backed value object property with a storage conversion.
    /// </summary>
    public static PropertyBuilder<TVO> IsValueObject<TVO>(this PropertyBuilder<TVO> builder)
        where TVO : ValueObject<Guid>, IReconstructable<TVO, Guid>
    {
        // Capture static abstract as delegate — expression trees cannot call them directly (CS8927).
        Func<Guid, TVO> reconstruct = TVO.Reconstruct;
        builder.HasConversion(vo => vo.Value, value => reconstruct(value));
        return builder;
    }

    /// <summary>
    /// Configures a required <see cref="string"/>-backed value object property with a storage
    /// conversion, max length, and <c>IsRequired</c> constraint.
    /// </summary>
    public static PropertyBuilder<TVO> IsValueObject<TVO>(this PropertyBuilder<TVO> builder, int maxLength)
        where TVO : ValueObject<string>, IReconstructable<TVO, string>
    {
        Func<string, TVO> reconstruct = TVO.Reconstruct;
        builder.HasConversion(vo => vo.Value, value => reconstruct(value));
        builder.HasMaxLength(maxLength);
        builder.IsRequired();
        return builder;
    }

    /// <summary>
    /// Configures a nullable <see cref="string"/>-backed value object property with a null-safe
    /// storage conversion and max length.
    /// </summary>
    public static PropertyBuilder<TVO?> IsNullableValueObject<TVO>(this PropertyBuilder<TVO?> builder, int maxLength)
        where TVO : ValueObject<string>, IReconstructable<TVO, string>
    {
        Func<string, TVO> reconstruct = TVO.Reconstruct;
        builder.HasConversion(
            vo => vo != null ? vo.Value : null,
            value => value != null ? reconstruct(value) : null);
        builder.HasMaxLength(maxLength);
        return builder;
    }
}
