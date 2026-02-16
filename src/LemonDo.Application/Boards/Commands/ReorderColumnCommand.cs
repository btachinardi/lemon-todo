namespace LemonDo.Application.Boards.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using Microsoft.Extensions.Logging;

/// <summary>Command to change a column's position on a board.</summary>
public sealed record ReorderColumnCommand(Guid BoardId, Guid ColumnId, int NewPosition);

/// <summary>Moves the column to the new position via <see cref="LemonDo.Domain.Boards.Entities.Board.ReorderColumn"/>.</summary>
public sealed class ReorderColumnCommandHandler(IBoardRepository repository, IUnitOfWork unitOfWork, ILogger<ReorderColumnCommandHandler> logger)
{
    /// <summary>Loads the board, moves the column to the new position, and persists the change.</summary>
    public async Task<Result<ColumnDto, DomainError>> HandleAsync(ReorderColumnCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Reordering column {ColumnId} to position {NewPosition} on board {BoardId}", command.ColumnId, command.NewPosition, command.BoardId);

        var board = await repository.GetByIdAsync(BoardId.From(command.BoardId), ct);
        if (board is null)
        {
            var error = DomainError.NotFound("Board", command.BoardId.ToString());
            logger.LogWarning("Failed to reorder column: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            return Result<ColumnDto, DomainError>.Failure(error);
        }

        var result = board.ReorderColumn(ColumnId.From(command.ColumnId), command.NewPosition);
        if (result.IsFailure)
            return Result<ColumnDto, DomainError>.Failure(result.Error);

        await repository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var column = board.Columns.First(c => c.Id == ColumnId.From(command.ColumnId));
        logger.LogInformation("Column {ColumnId} reordered to position {NewPosition} on board {BoardId} successfully", command.ColumnId, command.NewPosition, command.BoardId);
        return Result<ColumnDto, DomainError>.Success(new ColumnDto
        {
            Id = column.Id.Value,
            Name = column.Name.Value,
            TargetStatus = column.TargetStatus.ToString(),
            Position = column.Position,
            MaxTasks = column.MaxTasks
        });
    }
}
