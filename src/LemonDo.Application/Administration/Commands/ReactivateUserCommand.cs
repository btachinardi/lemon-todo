namespace LemonDo.Application.Administration.Commands;

using LemonDo.Domain.Common;

/// <summary>Command to reactivate a deactivated user account. Requires SystemAdmin.</summary>
public sealed record ReactivateUserCommand(Guid UserId);

/// <summary>Handles <see cref="ReactivateUserCommand"/> via the admin user service.</summary>
public sealed class ReactivateUserCommandHandler(IAdminUserService adminUserService, IAuditService auditService)
{
    /// <summary>Reactivates the user and creates an audit entry.</summary>
    public async Task<Result<DomainError>> HandleAsync(ReactivateUserCommand command, CancellationToken ct = default)
    {
        var result = await adminUserService.ReactivateUserAsync(command.UserId, ct);
        if (result.IsSuccess)
        {
            await auditService.RecordAsync(
                Domain.Administration.AuditAction.UserReactivated,
                "User", command.UserId.ToString(),
                "User reactivated", cancellationToken: ct);
        }

        return result;
    }
}
