namespace LemonDo.Domain.Common;

/// <summary>
/// Base class for value objects. Equality is determined by structural equality
/// of the components returned by <see cref="GetEqualityComponents"/>.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>Returns the values that define structural equality for this value object.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        return Equals((ValueObject)obj);
    }

    /// <inheritdoc />
    public bool Equals(ValueObject? other)
    {
        if (other is null || other.GetType() != GetType())
            return false;

        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(0, (hash, component) =>
                HashCode.Combine(hash, component));
    }

    /// <inheritdoc />
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <inheritdoc />
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}
