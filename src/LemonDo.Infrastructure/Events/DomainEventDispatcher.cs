namespace LemonDo.Infrastructure.Events;

using LemonDo.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dispatches domain events to their registered <see cref="IDomainEventHandler{TEvent}"/> implementations.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<DomainEvent> events, CancellationToken ct = default);
}

/// <summary>
/// Resolves handlers from DI using reflection to match the concrete event type to
/// <see cref="IDomainEventHandler{TEvent}"/>. Events are dispatched sequentially in order.
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
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
