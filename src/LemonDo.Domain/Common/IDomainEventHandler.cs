namespace LemonDo.Domain.Common;

/// <summary>
/// Handles a specific domain event type after the originating transaction commits.
/// Implementations are resolved from DI and invoked by the infrastructure event dispatcher.
/// </summary>
/// <typeparam name="TEvent">The domain event type to handle.</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : DomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}
