namespace LemonDo.Infrastructure.Identity;

using LemonDo.Application.Administration.DTOs;
using LemonDo.Application.Administration.Queries;
using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Entities;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Infrastructure implementation of <see cref="IAdminUserQuery"/>.
/// Queries the domain <c>Users</c> table for profile data and ASP.NET Identity for roles.
/// All returned PII is in redacted form. Search supports exact email match via hash
/// or partial match on redacted display name.
/// </summary>
public sealed class AdminUserQuery(
    LemonDoDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IAdminUserQuery
{
    /// <inheritdoc />
    public async Task<PagedResult<AdminUserDto>> ListUsersAsync(
        string? search, string? role, int page, int pageSize, CancellationToken ct)
    {
        var query = dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Exact email match via hash (shadow property) or partial match on redacted name
            var emailHash = PiiHasher.HashEmail(search);
            var term = search.Trim().ToLower();

            query = query.Where(u =>
                EF.Property<string>(u, "EmailHash") == emailHash ||
                u.RedactedDisplayName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var usersInRole = await userManager.GetUsersInRoleAsync(role);
            var userIdsInRole = usersInRole.Select(u => UserId.Reconstruct(u.Id)).ToList();
            query = query.Where(u => userIdsInRole.Contains(u.Id));
        }

        var totalCount = await query.CountAsync(ct);

        var users = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = new List<AdminUserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await GetRolesAsync(user.Id.Value);
            dtos.Add(MapToDto(user, roles));
        }

        return new PagedResult<AdminUserDto>(dtos, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<Result<AdminUserDto, DomainError>> GetUserByIdAsync(Guid userId, CancellationToken ct)
    {
        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == UserId.Reconstruct(userId), ct);

        if (user is null)
            return Result<AdminUserDto, DomainError>.Failure(
                DomainError.NotFound("user", userId.ToString()));

        var roles = await GetRolesAsync(userId);
        return Result<AdminUserDto, DomainError>.Success(MapToDto(user, roles));
    }

    private async Task<IList<string>> GetRolesAsync(Guid userId)
    {
        var identityUser = await userManager.FindByIdAsync(userId.ToString());
        return identityUser is not null
            ? await userManager.GetRolesAsync(identityUser)
            : Array.Empty<string>();
    }

    private static AdminUserDto MapToDto(User user, IList<string> roles) => new()
    {
        Id = user.Id.Value,
        Email = user.RedactedEmail,
        DisplayName = user.RedactedDisplayName,
        Roles = roles.ToList().AsReadOnly(),
        IsActive = !user.IsDeactivated,
        CreatedAt = user.CreatedAt,
    };
}
