namespace LemonDo.Domain.Common;

/// <summary>
/// Base class for all domain events.
/// </summary>
public abstract record DomainEvent
{
    /// <summary>Unique identifier for this event instance.</summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>UTC timestamp when the event was raised.</summary>
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
