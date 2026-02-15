namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Raised when a tag is added to a task via <see cref="Entities.Task.AddTag"/>.</summary>
public sealed record TaskTagAddedEvent(TaskId TaskId, Tag Tag) : DomainEvent;
