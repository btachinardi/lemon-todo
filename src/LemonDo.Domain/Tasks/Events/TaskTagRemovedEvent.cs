namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Raised when a tag is removed from a task via <see cref="Entities.Task.RemoveTag"/>.</summary>
public sealed record TaskTagRemovedEvent(TaskId TaskId, Tag Tag) : DomainEvent;
