namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Raised when a scalar field (title, description) is updated on a task.</summary>
public sealed record TaskUpdatedEvent(
    TaskId TaskId,
    string FieldName,
    string? OldValue,
    string? NewValue) : DomainEvent;
