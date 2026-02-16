namespace LemonDo.Application.Administration.Commands;

using LemonDo.Domain.Common;

/// <summary>Command to remove a role from a user. Requires SystemAdmin.</summary>
public sealed record RemoveRoleCommand(Guid UserId, string RoleName);

/// <summary>Handles <see cref="RemoveRoleCommand"/> via the admin user service.</summary>
public sealed class RemoveRoleCommandHandler(IAdminUserService adminUserService, IAuditService auditService)
{
    /// <summary>Removes the role and creates an audit entry.</summary>
    public async Task<Result<DomainError>> HandleAsync(RemoveRoleCommand command, CancellationToken ct = default)
    {
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
