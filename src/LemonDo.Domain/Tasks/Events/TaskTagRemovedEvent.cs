namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskTagRemovedEvent(TaskId TaskId, Tag Tag) : DomainEvent;
