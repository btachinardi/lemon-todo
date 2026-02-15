namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Raised when a task is soft-deleted via <see cref="Entities.Task.Delete"/>.</summary>
public sealed record TaskDeletedEvent(
    TaskId TaskId,
    DateTimeOffset DeletedAt) : DomainEvent;
