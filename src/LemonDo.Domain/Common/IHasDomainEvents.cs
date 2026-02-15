namespace LemonDo.Domain.Common;

/// <summary>
/// Marks an entity as capable of raising domain events.
/// Events are collected during mutations and dispatched after the unit of work commits the transaction.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>Domain events raised by this entity during the current unit of work. Cleared after dispatch.</summary>
    IReadOnlyList<DomainEvent> DomainEvents { get; }

    /// <summary>Clears all pending domain events. Called by the infrastructure after successful dispatch.</summary>
    void ClearDomainEvents();
}
