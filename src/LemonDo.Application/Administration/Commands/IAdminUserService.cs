namespace LemonDo.Application.Administration.Commands;

using LemonDo.Domain.Common;

/// <summary>
/// Port for admin user management operations. Implemented in Infrastructure layer
/// using ASP.NET Identity's UserManager.
/// </summary>
public interface IAdminUserService
{
    /// <summary>Assigns a role to a user.</summary>
    Task<Result<DomainError>> AssignRoleAsync(Guid userId, string roleName, CancellationToken ct = default);

    /// <summary>Removes a role from a user.</summary>
    Task<Result<DomainError>> RemoveRoleAsync(Guid userId, string roleName, CancellationToken ct = default);

    /// <summary>Deactivates a user account (permanent lockout).</summary>
    Task<Result<DomainError>> DeactivateUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Reactivates a deactivated user account.</summary>
    Task<Result<DomainError>> ReactivateUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Verifies the admin user's own password for re-authentication (break-the-glass).</summary>
    Task<Result<DomainError>> VerifyAdminPasswordAsync(Guid adminUserId, string password, CancellationToken ct = default);

    /// <summary>Reveals a user's decrypted PII (email and display name).</summary>
    Task<Result<RevealedPiiDto, DomainError>> RevealPiiAsync(Guid userId, CancellationToken ct = default);
}
