namespace LemonDo.Application.Administration.EventHandlers;

using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Events;

/// <summary>Records an audit entry when a task is created.</summary>
public sealed class AuditOnTaskCreated(IAuditService auditService) : IDomainEventHandler<TaskCreatedEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(TaskCreatedEvent domainEvent, CancellationToken ct = default)
    {
        await auditService.RecordAsync(
            AuditAction.TaskCreated,
            "Task",
            domainEvent.TaskId.Value.ToString(),
            $"Title: {domainEvent.Title}",
            actorIdOverride: domainEvent.OwnerId.Value,
            cancellationToken: ct);
    }
}
