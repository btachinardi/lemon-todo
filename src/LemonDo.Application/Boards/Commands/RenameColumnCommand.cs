namespace LemonDo.Application.Boards.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using Microsoft.Extensions.Logging;

/// <summary>Command to rename a column on a board.</summary>
public sealed record RenameColumnCommand(Guid BoardId, Guid ColumnId, string Name);

/// <summary>Validates the new name and delegates to <see cref="LemonDo.Domain.Boards.Entities.Board.RenameColumn"/>.</summary>
public sealed class RenameColumnCommandHandler(IBoardRepository repository, IUnitOfWork unitOfWork, ILogger<RenameColumnCommandHandler> logger)
{
    /// <summary>Validates the new name, loads the board, renames the column, and persists the change.</summary>
    public async Task<Result<ColumnDto, DomainError>> HandleAsync(RenameColumnCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Renaming column {ColumnId} to {ColumnName} on board {BoardId}", command.ColumnId, command.Name, command.BoardId);

        var board = await repository.GetByIdAsync(BoardId.From(command.BoardId), ct);
        if (board is null)
        {
            var error = DomainError.NotFound("Board", command.BoardId.ToString());
            logger.LogWarning("Failed to rename column: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            return Result<ColumnDto, DomainError>.Failure(error);
        }

        var nameResult = ColumnName.Create(command.Name);
        if (nameResult.IsFailure)
            return Result<ColumnDto, DomainError>.Failure(nameResult.Error);

        var result = board.RenameColumn(ColumnId.From(command.ColumnId), nameResult.Value);
        if (result.IsFailure)
            return Result<ColumnDto, DomainError>.Failure(result.Error);

        await repository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var column = board.Columns.First(c => c.Id == ColumnId.From(command.ColumnId));
        logger.LogInformation("Column {ColumnId} renamed to {ColumnName} on board {BoardId} successfully", command.ColumnId, command.Name, command.BoardId);
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
