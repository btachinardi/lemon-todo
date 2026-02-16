namespace LemonDo.Api.Endpoints;

using LemonDo.Api.Contracts;
using LemonDo.Api.Extensions;
using LemonDo.Application.Boards.Commands;
using LemonDo.Application.Tasks.Queries;

/// <summary>Minimal API endpoint definitions for the <c>/api/boards</c> route group.</summary>
public static class BoardEndpoints
{
    /// <summary>
    /// Maps board query and column management endpoints under <c>/api/boards</c> including
    /// default board retrieval, board lookup, and column operations (add, rename, remove, reorder).
    /// </summary>
    /// <returns>The route group builder for method chaining.</returns>
    public static RouteGroupBuilder MapBoardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/boards").WithTags("Boards").RequireAuthorization();

        group.MapGet("/default", GetDefaultBoard);
        group.MapGet("/{id:guid}", GetBoardById);
        group.MapPost("/{id:guid}/columns", AddColumn);
        group.MapPut("/{id:guid}/columns/{colId:guid}", RenameColumn);
        group.MapDelete("/{id:guid}/columns/{colId:guid}", RemoveColumn);
        group.MapPost("/{id:guid}/columns/reorder", ReorderColumn);

        return group;
    }

    private static async Task<IResult> GetDefaultBoard(
        GetDefaultBoardQueryHandler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(ct);
        return result.ToHttpResult(httpContext: httpContext);
    }

    private static async Task<IResult> GetBoardById(
        GetBoardQueryHandler handler,
        Guid id,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetBoardQuery(id), ct);
        return result.ToHttpResult(httpContext: httpContext);
    }

    private static async Task<IResult> AddColumn(
        AddColumnCommandHandler handler,
        Guid id,
        AddColumnRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var command = new AddColumnCommand(id, request.Name, request.TargetStatus, request.Position);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(dto => Results.Created($"/api/boards/{id}/columns/{dto.Id}", dto), httpContext: httpContext);
    }

    private static async Task<IResult> RenameColumn(
        RenameColumnCommandHandler handler,
        Guid id,
        Guid colId,
        RenameColumnRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var command = new RenameColumnCommand(id, colId, request.Name);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(httpContext: httpContext);
    }

    private static async Task<IResult> RemoveColumn(
        RemoveColumnCommandHandler handler,
        Guid id,
        Guid colId,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var command = new RemoveColumnCommand(id, colId);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(() => Results.Ok(new { Success = true }), httpContext: httpContext);
    }

    private static async Task<IResult> ReorderColumn(
        ReorderColumnCommandHandler handler,
        Guid id,
        ReorderColumnRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var command = new ReorderColumnCommand(id, request.ColumnId, request.NewPosition);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(httpContext: httpContext);
    }
}
