namespace LemonDo.Application.Administration.Queries;

using LemonDo.Application.Administration.DTOs;
using LemonDo.Domain.Common;

/// <summary>
/// Port for admin user queries. Implemented in Infrastructure layer
/// using ASP.NET Identity's UserManager.
/// </summary>
public interface IAdminUserQuery
{
    /// <summary>Lists users with optional search/role filter, paginated, protected data redacted.</summary>
    Task<PagedResult<AdminUserDto>> ListUsersAsync(
        string? search, string? role, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Gets a single user by ID with redacted protected data.</summary>
    Task<Result<AdminUserDto, DomainError>> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
}
