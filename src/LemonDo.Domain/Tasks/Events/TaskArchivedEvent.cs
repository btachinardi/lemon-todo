namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Raised when a task is archived via <see cref="Entities.Task.Archive"/>.</summary>
public sealed record TaskArchivedEvent(TaskId TaskId) : DomainEvent;
