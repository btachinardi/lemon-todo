namespace LemonDo.Infrastructure.Events;

using LemonDo.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
/// Logs each handler invocation at debug level and logs errors with structured context before re-throwing.
/// </summary>
/// <param name="serviceProvider">The service provider used to resolve event handlers from the DI container.</param>
/// <param name="logger">The logger used to trace handler invocations and record dispatch failures.</param>
public sealed class DomainEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
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
                    var handlerTypeName = handler!.GetType().Name;
                    var eventTypeName = eventType.Name;

                    logger.LogDebug(
                        "Dispatching {EventType} to {HandlerType}",
                        eventTypeName,
                        handlerTypeName);

                    try
                    {
                        var task = (Task)method.Invoke(handler, [domainEvent, ct])!;
                        await task;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Failed to dispatch {EventType} to {HandlerType}",
                            eventTypeName,
                            handlerTypeName);

                        throw;
                    }
                }
            }
        }
    }
}
