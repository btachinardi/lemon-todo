namespace LemonDo.Infrastructure.Identity;

using LemonDo.Application.Administration.Commands;
using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Infrastructure.Resilience;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

/// <summary>
/// Infrastructure implementation of <see cref="IAdminUserService"/>.
/// Role management uses ASP.NET Identity directly. Deactivation coordinates
/// the domain <see cref="LemonDo.Domain.Identity.Entities.User"/> with Identity lockout.
/// </summary>
/// <remarks>
/// Identity operations go through EF Core internally. Under high concurrency on SQLite
/// (shared in-memory connection), transient errors can occur. The <see cref="TransientFaultRetryPolicy"/>
/// retries the full operation when a transient SQLite fault is detected, ensuring that
/// connection-state errors result in 409 (via the caller's domain error) rather than 500.
/// </remarks>
public sealed class AdminUserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    TransientFaultRetryPolicy retryPolicy,
    ILogger<AdminUserService> logger) : IAdminUserService
{
    /// <inheritdoc />
    public async Task<Result<DomainError>> AssignRoleAsync(Guid userId, string roleName, CancellationToken ct)
    {
        return await retryPolicy.ExecuteAsync(async () =>
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
        }, ct);
    }

    /// <inheritdoc />
    public async Task<Result<DomainError>> RemoveRoleAsync(Guid userId, string roleName, CancellationToken ct)
    {
        return await retryPolicy.ExecuteAsync(async () =>
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
        }, ct);
    }

    /// <inheritdoc />
    public async Task<Result<DomainError>> DeactivateUserAsync(Guid userId, CancellationToken ct)
    {
        return await retryPolicy.ExecuteAsync(async () =>
        {
            var domainUser = await userRepository.GetByIdAsync(UserId.Reconstruct(userId), ct);
            if (domainUser is null)
                return Result<DomainError>.Failure(DomainError.NotFound("user", userId.ToString()));

            var result = domainUser.Deactivate();
            if (result.IsFailure)
                return result;

            await userRepository.UpdateAsync(domainUser, ct);
            await unitOfWork.SaveChangesAsync(ct);

            // Also lock out in Identity to prevent login
            var identityUser = await userManager.FindByIdAsync(userId.ToString());
            if (identityUser is not null)
                await userManager.SetLockoutEndDateAsync(identityUser, DateTimeOffset.MaxValue);

            return Result<DomainError>.Success();
        }, ct);
    }

    /// <inheritdoc />
    public async Task<Result<DomainError>> ReactivateUserAsync(Guid userId, CancellationToken ct)
    {
        return await retryPolicy.ExecuteAsync(async () =>
        {
            var domainUser = await userRepository.GetByIdAsync(UserId.Reconstruct(userId), ct);
            if (domainUser is null)
                return Result<DomainError>.Failure(DomainError.NotFound("user", userId.ToString()));

            var result = domainUser.Reactivate();
            if (result.IsFailure)
                return result;

            await userRepository.UpdateAsync(domainUser, ct);
            await unitOfWork.SaveChangesAsync(ct);

            // Clear Identity lockout
            var identityUser = await userManager.FindByIdAsync(userId.ToString());
            if (identityUser is not null)
            {
                await userManager.SetLockoutEndDateAsync(identityUser, null);
                await userManager.ResetAccessFailedCountAsync(identityUser);
            }

            return Result<DomainError>.Success();
        }, ct);
    }
}
