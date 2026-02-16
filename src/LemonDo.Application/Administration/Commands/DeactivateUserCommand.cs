namespace LemonDo.Application.Administration.Commands;

using LemonDo.Domain.Common;

/// <summary>Command to deactivate a user account. Requires SystemAdmin.</summary>
public sealed record DeactivateUserCommand(Guid UserId);

/// <summary>Handles <see cref="DeactivateUserCommand"/> via the admin user service.</summary>
public sealed class DeactivateUserCommandHandler(IAdminUserService adminUserService, IAuditService auditService)
{
    /// <summary>Deactivates the user and creates an audit entry.</summary>
    public async Task<Result<DomainError>> HandleAsync(DeactivateUserCommand command, CancellationToken ct = default)
    {
        var result = await adminUserService.DeactivateUserAsync(command.UserId, ct);
        if (result.IsSuccess)
        {
            await auditService.RecordAsync(
                Domain.Administration.AuditAction.UserDeactivated,
                "User", command.UserId.ToString(),
                "User deactivated", cancellationToken: ct);
        }

        return result;
    }
}
