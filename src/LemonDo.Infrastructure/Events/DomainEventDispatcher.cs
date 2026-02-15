namespace LemonDo.Infrastructure.Events;

using LemonDo.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dispatches domain events to their registered <see cref="IDomainEventHandler{TEvent}"/> implementations.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches a collection of domain events to all registered handlers. Events are dispatched
    /// sequentially in the order they appear in the collection.
    /// </summary>
    /// <param name="events">Domain events to dispatch, typically collected during an aggregate operation.</param>
    /// <param name="ct">Cancellation token to cancel the dispatch operation.</param>
    Task DispatchAsync(IReadOnlyList<DomainEvent> events, CancellationToken ct = default);
}

/// <summary>
/// Resolves handlers from DI using reflection to match the concrete event type to
/// <see cref="IDomainEventHandler{TEvent}"/>. Events are dispatched sequentially in order.
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    /// <inheritdoc/>
    public async Task DispatchAsync(IReadOnlyList<DomainEvent> events, CancellationToken ct = default)
    {
        foreach (var domainEvent in events)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                var method = handlerType.GetMethod("HandleAsync");
                if (method is not null)
                {
                    var task = (Task)method.Invoke(handler, [domainEvent, ct])!;
                    await task;
                }
            }
        }
    }
}
