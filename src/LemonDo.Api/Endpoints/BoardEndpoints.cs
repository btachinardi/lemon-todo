namespace LemonDo.Api.Endpoints;

using LemonDo.Api.Contracts;
using LemonDo.Api.Extensions;
using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Application.Tasks.Queries;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

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
        IBoardRepository repository,
        IUnitOfWork unitOfWork,
        Guid id,
        AddColumnRequest request,
        CancellationToken ct)
    {
        var board = await repository.GetByIdAsync(BoardId.From(id), ct);
        if (board is null)
            return DomainError.NotFound("Board", id.ToString()).ToNotFoundResult();

        var nameResult = ColumnName.Create(request.Name);
        if (nameResult.IsFailure)
            return Result<DomainError>.Failure(nameResult.Error).ToHttpResult();

        if (!Enum.TryParse<BoardTaskStatus>(request.TargetStatus, true, out var targetStatus))
            return Results.BadRequest(new { Error = $"Invalid target status: '{request.TargetStatus}'. Valid values: Todo, InProgress, Done." });

        var result = board.AddColumn(nameResult.Value, targetStatus, request.Position);
        if (result.IsFailure)
            return result.ToHttpResult();

        await repository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var addedColumn = board.Columns.First(c => c.Name.Value == nameResult.Value.Value);
        return Results.Created($"/api/boards/{id}/columns/{addedColumn.Id.Value}", new ColumnDto
        {
            Id = addedColumn.Id.Value,
            Name = addedColumn.Name.Value,
            TargetStatus = addedColumn.TargetStatus.ToString(),
            Position = addedColumn.Position,
            MaxTasks = addedColumn.MaxTasks
        });
    }

    private static async Task<IResult> RenameColumn(
        IBoardRepository repository,
        IUnitOfWork unitOfWork,
        Guid id,
        Guid colId,
        RenameColumnRequest request,
        CancellationToken ct)
    {
        var board = await repository.GetByIdAsync(BoardId.From(id), ct);
        if (board is null)
            return DomainError.NotFound("Board", id.ToString()).ToNotFoundResult();

        var nameResult = ColumnName.Create(request.Name);
        if (nameResult.IsFailure)
            return Result<DomainError>.Failure(nameResult.Error).ToHttpResult();

        var result = board.RenameColumn(ColumnId.From(colId), nameResult.Value);
        if (result.IsFailure)
            return result.ToHttpResult();

        await repository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var column = board.Columns.First(c => c.Id == ColumnId.From(colId));
        return Results.Ok(new ColumnDto
        {
            Id = column.Id.Value,
            Name = column.Name.Value,
            TargetStatus = column.TargetStatus.ToString(),
            Position = column.Position,
            MaxTasks = column.MaxTasks
        });
    }

    private static async Task<IResult> RemoveColumn(
        IBoardRepository repository,
        IUnitOfWork unitOfWork,
        Guid id,
        Guid colId,
        CancellationToken ct)
    {
        var board = await repository.GetByIdAsync(BoardId.From(id), ct);
        if (board is null)
            return DomainError.NotFound("Board", id.ToString()).ToNotFoundResult();

        var result = board.RemoveColumn(ColumnId.From(colId));
        if (result.IsFailure)
            return result.ToHttpResult();

        await repository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Results.Ok(new { Success = true });
    }

    private static async Task<IResult> ReorderColumn(
        IBoardRepository repository,
        IUnitOfWork unitOfWork,
        Guid id,
        ReorderColumnRequest request,
        CancellationToken ct)
    {
        var board = await repository.GetByIdAsync(BoardId.From(id), ct);
        if (board is null)
            return DomainError.NotFound("Board", id.ToString()).ToNotFoundResult();

        var result = board.ReorderColumn(ColumnId.From(request.ColumnId), request.NewPosition);
        if (result.IsFailure)
            return result.ToHttpResult();

        await repository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var column = board.Columns.First(c => c.Id == ColumnId.From(request.ColumnId));
        return Results.Ok(new ColumnDto
        {
            Id = column.Id.Value,
            Name = column.Name.Value,
            TargetStatus = column.TargetStatus.ToString(),
            Position = column.Position,
            MaxTasks = column.MaxTasks
        });
    }

    private static IResult ToNotFoundResult(this DomainError error)
    {
        return Results.Json(new ErrorResponse(
            Type: "not_found",
            Title: error.Message,
            Status: 404,
            Errors: new Dictionary<string, string[]>
            {
                [error.Code.Split('.')[0]] = [error.Message]
            }), statusCode: 404);
    }
}
