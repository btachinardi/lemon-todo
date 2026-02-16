namespace LemonDo.Application.Administration.EventHandlers;

using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Events;

/// <summary>Records an audit entry when a user registers.</summary>
public sealed class AuditOnUserRegistered(IAuditService auditService) : IDomainEventHandler<UserRegisteredEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(UserRegisteredEvent domainEvent, CancellationToken ct = default)
    {
        await auditService.RecordAsync(
            AuditAction.UserRegistered,
            "User",
            domainEvent.UserId.Value.ToString(),
            cancellationToken: ct);
    }
}
