namespace LemonDo.Application.Administration.EventHandlers;

using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Events;

/// <summary>Records an audit entry when a task is deleted.</summary>
public sealed class AuditOnTaskDeleted(IAuditService auditService) : IDomainEventHandler<TaskDeletedEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(TaskDeletedEvent domainEvent, CancellationToken ct = default)
    {
        await auditService.RecordAsync(
            AuditAction.TaskDeleted,
            "Task",
            domainEvent.TaskId.Value.ToString(),
            cancellationToken: ct);
    }
}
