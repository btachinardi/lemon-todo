namespace LemonDo.Domain.Common;

/// <summary>
/// Base class for domain entities. Identity is determined by <see cref="Id"/>.
/// Supports domain event collection.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>, IHasDomainEvents where TId : notnull
{
    /// <summary>The unique identifier for this entity.</summary>
    public TId Id { get; }

    /// <summary>When the entity was first persisted (UTC).</summary>
    public DateTimeOffset CreatedAt { get; protected set; }

    /// <summary>When the entity was last modified (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; protected set; }

    private readonly List<DomainEvent> _domainEvents = [];

    /// <inheritdoc />
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Initializes a new entity with the given <paramref name="id"/> and timestamps set to now.</summary>
    protected Entity(TId id)
    {
        Id = id;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Adds a domain event to be dispatched after the unit of work commits.</summary>
    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc />
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;

        return Equals(other);
    }

    /// <inheritdoc />
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <inheritdoc />
    public override int GetHashCode() => Id.GetHashCode();

    /// <inheritdoc />
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <inheritdoc />
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }
}
