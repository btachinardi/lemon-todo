namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskDeletedEvent(
    TaskItemId TaskItemId,
    DateTimeOffset DeletedAt) : DomainEvent;
