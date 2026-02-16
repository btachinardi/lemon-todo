namespace LemonDo.Infrastructure.Identity;

using LemonDo.Application.Administration.Commands;
using LemonDo.Domain.Common;
using LemonDo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

/// <summary>
/// Infrastructure implementation of <see cref="IAdminUserService"/> using ASP.NET Identity.
/// </summary>
public sealed class AdminUserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    LemonDoDbContext dbContext,
    ILogger<AdminUserService> logger) : IAdminUserService
{
    /// <inheritdoc />
    public async Task<Result<DomainError>> AssignRoleAsync(Guid userId, string roleName, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result<DomainError>.Failure(DomainError.NotFound("user", userId.ToString()));

        if (!await roleManager.RoleExistsAsync(roleName))
            return Result<DomainError>.Failure(
                DomainError.Validation("role", $"Role '{roleName}' does not exist."));

        if (await userManager.IsInRoleAsync(user, roleName))
            return Result<DomainError>.Failure(
                DomainError.Conflict("role", $"User already has role '{roleName}'."));

        var result = await userManager.AddToRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            logger.LogWarning("Failed to assign role {Role} to user {UserId}: {Errors}",
                roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("role.assignment", "Failed to assign role."));
        }

        return Result<DomainError>.Success();
    }

    /// <inheritdoc />
    public async Task<Result<DomainError>> RemoveRoleAsync(Guid userId, string roleName, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result<DomainError>.Failure(DomainError.NotFound("user", userId.ToString()));

        if (!await userManager.IsInRoleAsync(user, roleName))
            return Result<DomainError>.Failure(
                DomainError.Validation("role", $"User does not have role '{roleName}'."));

        var result = await userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            logger.LogWarning("Failed to remove role {Role} from user {UserId}: {Errors}",
                roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("role.removal", "Failed to remove role."));
        }

        return Result<DomainError>.Success();
    }

    /// <inheritdoc />
    public async Task<Result<DomainError>> DeactivateUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result<DomainError>.Failure(DomainError.NotFound("user", userId.ToString()));

        if (user.IsDeactivated)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("user.deactivation", "User is already deactivated."));

        user.IsDeactivated = true;
        // Set lockout to far future to prevent login
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        await dbContext.SaveChangesAsync(ct);

        return Result<DomainError>.Success();
    }

    /// <inheritdoc />
    public async Task<Result<DomainError>> ReactivateUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result<DomainError>.Failure(DomainError.NotFound("user", userId.ToString()));

        if (!user.IsDeactivated)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("user.reactivation", "User is not deactivated."));

        user.IsDeactivated = false;
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        await dbContext.SaveChangesAsync(ct);

        return Result<DomainError>.Success();
    }
}
