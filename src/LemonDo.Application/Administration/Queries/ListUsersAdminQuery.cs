namespace LemonDo.Application.Administration.Queries;

using LemonDo.Application.Administration.DTOs;
using LemonDo.Domain.Common;

/// <summary>Query to list users for admin views with optional filtering.</summary>
public sealed record ListUsersAdminQuery(
    string? Search = null,
    string? Role = null,
    int Page = 1,
    int PageSize = 20);

/// <summary>Handles <see cref="ListUsersAdminQuery"/> by querying Identity users with redacted PII.</summary>
public sealed class ListUsersAdminQueryHandler(IAdminUserQuery adminUserQuery)
{
    /// <summary>Returns a paginated list of users with redacted PII.</summary>
    public Task<PagedResult<AdminUserDto>> HandleAsync(ListUsersAdminQuery query, CancellationToken ct = default)
        => adminUserQuery.ListUsersAsync(query.Search, query.Role, query.Page, query.PageSize, ct);
}
