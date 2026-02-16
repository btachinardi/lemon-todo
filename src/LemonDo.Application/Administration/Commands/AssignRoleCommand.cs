namespace LemonDo.Application.Administration.Commands;

using LemonDo.Domain.Common;

/// <summary>Command to assign a role to a user. Requires SystemAdmin.</summary>
public sealed record AssignRoleCommand(Guid UserId, string RoleName);

/// <summary>Handles <see cref="AssignRoleCommand"/> via the admin user service.</summary>
public sealed class AssignRoleCommandHandler(IAdminUserService adminUserService, IAuditService auditService)
{
    /// <summary>Assigns the role and creates an audit entry.</summary>
    public async Task<Result<DomainError>> HandleAsync(AssignRoleCommand command, CancellationToken ct = default)
    {
        var result = await adminUserService.AssignRoleAsync(command.UserId, command.RoleName, ct);
        if (result.IsSuccess)
        {
            await auditService.RecordAsync(
                Domain.Administration.AuditAction.RoleAssigned,
                "User", command.UserId.ToString(),
                $"Role '{command.RoleName}' assigned", cancellationToken: ct);
        }

        return result;
    }
}
