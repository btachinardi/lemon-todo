namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskUpdatedEvent(
    TaskItemId TaskItemId,
    string FieldName,
    string? OldValue,
    string? NewValue) : DomainEvent;
