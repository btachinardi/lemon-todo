namespace LemonDo.Application.Administration.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;

/// <summary>Command to remove a role from a user. Requires SystemAdmin.</summary>
public sealed record RemoveRoleCommand(Guid UserId, string RoleName);

/// <summary>Handles <see cref="RemoveRoleCommand"/> via the admin user service.</summary>
public sealed class RemoveRoleCommandHandler(
    IAdminUserService adminUserService,
    IAuditService auditService,
    IRequestContext requestContext)
{
    /// <summary>Removes the role and creates an audit entry.</summary>
    public async Task<Result<DomainError>> HandleAsync(RemoveRoleCommand command, CancellationToken ct = default)
    {
        // Guard: a SystemAdmin must not be able to remove roles from their own account.
        // Self-role-removal could silently de-escalate privileges or break access control invariants.
        if (requestContext.UserId.HasValue && requestContext.UserId.Value == command.UserId)
        {
            return Result<DomainError>.Failure(
                DomainError.BusinessRule(
                    "admin.self_role_removal",
                    "Cannot remove roles from your own account."));
        }

        var result = await adminUserService.RemoveRoleAsync(command.UserId, command.RoleName, ct);
        if (result.IsSuccess)
        {
            await auditService.RecordAsync(
                Domain.Administration.AuditAction.RoleRemoved,
                "User", command.UserId.ToString(),
                $"Role '{command.RoleName}' removed", cancellationToken: ct);
        }

        return result;
    }
}
