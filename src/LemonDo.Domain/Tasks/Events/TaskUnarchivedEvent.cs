namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Raised when a task is restored from archive via <see cref="Entities.Task.Unarchive"/>.</summary>
public sealed record TaskUnarchivedEvent(TaskId TaskId) : DomainEvent;
