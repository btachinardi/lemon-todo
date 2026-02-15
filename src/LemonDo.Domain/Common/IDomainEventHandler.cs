namespace LemonDo.Domain.Common;

/// <summary>
/// Handles a specific domain event type after the originating transaction commits.
/// Implementations are resolved from DI and invoked by the infrastructure event dispatcher.
/// </summary>
/// <typeparam name="TEvent">The domain event type to handle.</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : DomainEvent
{
    /// <summary>
    /// Handles the domain event asynchronously. Called by the infrastructure dispatcher after the
    /// transaction commits. Should be idempotent if possible, since event dispatch is at-least-once.
    /// </summary>
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}
