namespace LemonDo.Infrastructure.Identity;

using LemonDo.Application.Administration.DTOs;
using LemonDo.Application.Administration.Queries;
using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Infrastructure implementation of <see cref="IAdminUserQuery"/> using ASP.NET Identity.
/// Returns users with redacted PII by default.
/// </summary>
public sealed class AdminUserQuery(
    UserManager<ApplicationUser> userManager) : IAdminUserQuery
{
    /// <inheritdoc />
    public async Task<PagedResult<AdminUserDto>> ListUsersAsync(
        string? search, string? role, int page, int pageSize, CancellationToken ct)
    {
        var query = userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email!.ToLower().Contains(term) ||
                u.DisplayName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var usersInRole = await userManager.GetUsersInRoleAsync(role);
            var userIds = usersInRole.Select(u => u.Id).ToHashSet();
            query = query.Where(u => userIds.Contains(u.Id));
        }

        var totalCount = await query.CountAsync(ct);

        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = new List<AdminUserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            dtos.Add(MapToDto(user, roles));
        }

        return new PagedResult<AdminUserDto>(dtos, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<Result<AdminUserDto, DomainError>> GetUserByIdAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result<AdminUserDto, DomainError>.Failure(
                DomainError.NotFound("user", userId.ToString()));

        var roles = await userManager.GetRolesAsync(user);
        return Result<AdminUserDto, DomainError>.Success(MapToDto(user, roles));
    }

    private static AdminUserDto MapToDto(ApplicationUser user, IList<string> roles) => new()
    {
        Id = user.Id,
        Email = PiiRedactor.RedactEmail(user.Email ?? ""),
        DisplayName = PiiRedactor.RedactName(user.DisplayName),
        Roles = roles.ToList().AsReadOnly(),
        IsActive = !user.IsDeactivated,
        CreatedAt = user.CreatedAt,
    };
}
