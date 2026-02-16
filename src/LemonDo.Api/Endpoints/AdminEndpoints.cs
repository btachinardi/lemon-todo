namespace LemonDo.Api.Endpoints;

using LemonDo.Api.Contracts.Admin;
using LemonDo.Api.Extensions;
using LemonDo.Application.Administration.Commands;
using LemonDo.Application.Administration.Queries;
using LemonDo.Application.Common;
using LemonDo.Domain.Administration;

/// <summary>Minimal API endpoint definitions for admin operations under <c>/api/admin</c>.</summary>
public static class AdminEndpoints
{
    /// <summary>Maps all admin endpoints under <c>/api/admin</c>.</summary>
    public static RouteGroupBuilder MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization(Roles.RequireAdminOrAbove);

        // User management
        group.MapGet("/users", ListUsers);
        group.MapGet("/users/{id:guid}", GetUser);
        group.MapPost("/users/{id:guid}/roles", AssignRole)
            .RequireAuthorization(Roles.RequireSystemAdmin);
        group.MapDelete("/users/{id:guid}/roles/{roleName}", RemoveRole)
            .RequireAuthorization(Roles.RequireSystemAdmin);
        group.MapPost("/users/{id:guid}/deactivate", DeactivateUser)
            .RequireAuthorization(Roles.RequireSystemAdmin);
        group.MapPost("/users/{id:guid}/reactivate", ReactivateUser)
            .RequireAuthorization(Roles.RequireSystemAdmin);
        group.MapPost("/users/{id:guid}/reveal", RevealPii)
            .RequireAuthorization(Roles.RequireSystemAdmin);

        // Audit log
        group.MapGet("/audit", SearchAuditLog);

        return group;
    }

    private static async Task<IResult> ListUsers(
        ListUsersAdminQueryHandler handler,
        string? search = null,
        string? role = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await handler.HandleAsync(
            new ListUsersAdminQuery(search, role, page, pageSize), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetUser(
        GetUserAdminQueryHandler handler,
        Guid id,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetUserAdminQuery(id), ct);
        return result.ToHttpResult(httpContext: httpContext);
    }

    private static async Task<IResult> AssignRole(
        AssignRoleCommandHandler handler,
        Guid id,
        AssignRoleRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new AssignRoleCommand(id, request.RoleName), ct);
        return result.ToHttpResult(() => Results.Ok(new { Success = true }), httpContext: httpContext);
    }

    private static async Task<IResult> RemoveRole(
        RemoveRoleCommandHandler handler,
        Guid id,
        string roleName,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new RemoveRoleCommand(id, roleName), ct);
        return result.ToHttpResult(() => Results.Ok(new { Success = true }), httpContext: httpContext);
    }

    private static async Task<IResult> DeactivateUser(
        DeactivateUserCommandHandler handler,
        Guid id,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new DeactivateUserCommand(id), ct);
        return result.ToHttpResult(() => Results.Ok(new { Success = true }), httpContext: httpContext);
    }

    private static async Task<IResult> ReactivateUser(
        ReactivateUserCommandHandler handler,
        Guid id,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ReactivateUserCommand(id), ct);
        return result.ToHttpResult(() => Results.Ok(new { Success = true }), httpContext: httpContext);
    }

    private static async Task<IResult> SearchAuditLog(
        SearchAuditLogQueryHandler handler,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        AuditAction? action = null,
        Guid? actorId = null,
        string? resourceType = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await handler.HandleAsync(
            new SearchAuditLogQuery(dateFrom, dateTo, action, actorId, resourceType, page, pageSize), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> RevealPii(
        RevealPiiCommandHandler handler,
        Guid id,
        RevealPiiRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!Enum.TryParse<PiiRevealReason>(request.Reason, ignoreCase: true, out var reason))
            return Results.BadRequest(new { Error = $"Invalid reason: '{request.Reason}'." });

        var command = new RevealPiiCommand(id, reason, request.ReasonDetails, request.Comments, request.Password);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(dto => Results.Ok(dto), httpContext: httpContext);
    }
}
