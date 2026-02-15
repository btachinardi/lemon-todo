namespace LemonDo.Application.Boards.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using Microsoft.Extensions.Logging;

/// <summary>Command to remove a column from a board.</summary>
public sealed record RemoveColumnCommand(Guid BoardId, Guid ColumnId);

/// <summary>Removes the column via <see cref="LemonDo.Domain.Boards.Entities.Board.RemoveColumn"/>. Enforces minimum column invariants.</summary>
public sealed class RemoveColumnCommandHandler(IBoardRepository repository, IUnitOfWork unitOfWork, ILogger<RemoveColumnCommandHandler> logger)
{
    /// <inheritdoc/>
    public async Task<Result<DomainError>> HandleAsync(RemoveColumnCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Removing column {ColumnId} from board {BoardId}", command.ColumnId, command.BoardId);

        var board = await repository.GetByIdAsync(BoardId.From(command.BoardId), ct);
        if (board is null)
        {
            var error = DomainError.NotFound("Board", command.BoardId.ToString());
            logger.LogWarning("Failed to remove column: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            return Result<DomainError>.Failure(error);
        }

        var result = board.RemoveColumn(ColumnId.From(command.ColumnId));
        if (result.IsFailure)
            return result;

        await repository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Column {ColumnId} removed from board {BoardId} successfully", command.ColumnId, command.BoardId);
        return Result<DomainError>.Success();
    }
}
