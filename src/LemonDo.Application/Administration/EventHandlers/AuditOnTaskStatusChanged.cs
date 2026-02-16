namespace LemonDo.Application.Administration.EventHandlers;

using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Events;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Records an audit entry when a task is completed (status changed to Done).</summary>
public sealed class AuditOnTaskStatusChanged(IAuditService auditService) : IDomainEventHandler<TaskStatusChangedEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(TaskStatusChangedEvent domainEvent, CancellationToken ct = default)
    {
        if (domainEvent.NewStatus != TaskStatus.Done) return;

        await auditService.RecordAsync(
            AuditAction.TaskCompleted,
            "Task",
            domainEvent.TaskId.Value.ToString(),
            $"Status: {domainEvent.OldStatus} -> {domainEvent.NewStatus}",
            cancellationToken: ct);
    }
}
