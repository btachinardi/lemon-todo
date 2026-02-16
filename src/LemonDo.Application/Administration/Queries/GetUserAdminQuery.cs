namespace LemonDo.Application.Administration.Queries;

using LemonDo.Application.Administration.DTOs;
using LemonDo.Domain.Common;

/// <summary>Query to get a single user's details for admin views.</summary>
public sealed record GetUserAdminQuery(Guid UserId);

/// <summary>Handles <see cref="GetUserAdminQuery"/> by fetching a single user with redacted protected data.</summary>
public sealed class GetUserAdminQueryHandler(IAdminUserQuery adminUserQuery)
{
    /// <summary>Returns user details with redacted protected data, or a not-found error.</summary>
    public Task<Result<AdminUserDto, DomainError>> HandleAsync(GetUserAdminQuery query, CancellationToken ct = default)
        => adminUserQuery.GetUserByIdAsync(query.UserId, ct);
}
