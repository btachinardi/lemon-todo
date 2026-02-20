namespace LemonDo.Application.Administration.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;

/// <summary>Command to deactivate a user account. Requires SystemAdmin.</summary>
public sealed record DeactivateUserCommand(Guid UserId);

/// <summary>Handles <see cref="DeactivateUserCommand"/> via the admin user service.</summary>
public sealed class DeactivateUserCommandHandler(
    IAdminUserService adminUserService,
    IAuditService auditService,
    IRequestContext requestContext)
{
    /// <summary>Deactivates the user and creates an audit entry.</summary>
    public async Task<Result<DomainError>> HandleAsync(DeactivateUserCommand command, CancellationToken ct = default)
    {
        // Guard: a SystemAdmin must not be able to deactivate their own account.
        // Self-deactivation could leave the system with no active administrator.
        if (requestContext.UserId.HasValue && requestContext.UserId.Value == command.UserId)
        {
            return Result<DomainError>.Failure(
                DomainError.BusinessRule(
                    "admin.self_deactivation",
                    "Cannot deactivate your own account."));
        }

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
