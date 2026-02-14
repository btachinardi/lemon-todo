namespace LemonDo.Api.Endpoints;

using LemonDo.Api.Contracts;
using LemonDo.Api.Extensions;
using LemonDo.Application.Boards.Commands;
using LemonDo.Application.Tasks.Queries;

public static class BoardEndpoints
{
    public static RouteGroupBuilder MapBoardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/boards").WithTags("Boards");

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
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetBoardById(
        GetBoardQueryHandler handler,
        Guid id,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetBoardQuery(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> AddColumn(
        AddColumnCommandHandler handler,
        Guid id,
        AddColumnRequest request,
        CancellationToken ct)
    {
        var command = new AddColumnCommand(id, request.Name, request.TargetStatus, request.Position);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(dto => Results.Created($"/api/boards/{id}/columns/{dto.Id}", dto));
    }

    private static async Task<IResult> RenameColumn(
        RenameColumnCommandHandler handler,
        Guid id,
        Guid colId,
        RenameColumnRequest request,
        CancellationToken ct)
    {
        var command = new RenameColumnCommand(id, colId, request.Name);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RemoveColumn(
        RemoveColumnCommandHandler handler,
        Guid id,
        Guid colId,
        CancellationToken ct)
    {
        var command = new RemoveColumnCommand(id, colId);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(() => Results.Ok(new { Success = true }));
    }

    private static async Task<IResult> ReorderColumn(
        ReorderColumnCommandHandler handler,
        Guid id,
        ReorderColumnRequest request,
        CancellationToken ct)
    {
        var command = new ReorderColumnCommand(id, request.ColumnId, request.NewPosition);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult();
    }
}
