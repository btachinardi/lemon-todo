namespace LemonDo.Application.Administration.Commands;

using LemonDo.Domain.Common;

/// <summary>
/// Port for admin user management operations that require Identity coordination.
/// Role assignment uses Identity's UserManager. Deactivation coordinates domain User
/// state with Identity lockout. Password verification is on <see cref="LemonDo.Application.Identity.IAuthService"/>.
/// </summary>
public interface IAdminUserService
{
    /// <summary>Assigns a role to a user.</summary>
    Task<Result<DomainError>> AssignRoleAsync(Guid userId, string roleName, CancellationToken ct = default);

    /// <summary>Removes a role from a user.</summary>
    Task<Result<DomainError>> RemoveRoleAsync(Guid userId, string roleName, CancellationToken ct = default);

    /// <summary>Deactivates a user account: sets domain User.IsDeactivated + Identity lockout.</summary>
    Task<Result<DomainError>> DeactivateUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Reactivates a deactivated user: clears domain User.IsDeactivated + Identity lockout.</summary>
    Task<Result<DomainError>> ReactivateUserAsync(Guid userId, CancellationToken ct = default);
}
