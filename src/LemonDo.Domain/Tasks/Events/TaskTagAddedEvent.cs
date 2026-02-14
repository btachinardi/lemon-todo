namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskTagAddedEvent(TaskItemId TaskItemId, Tag Tag) : DomainEvent;
