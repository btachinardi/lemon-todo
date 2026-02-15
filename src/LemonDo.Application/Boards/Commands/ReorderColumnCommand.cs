namespace LemonDo.Application.Boards.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;

/// <summary>Command to change a column's position on a board.</summary>
public sealed record ReorderColumnCommand(Guid BoardId, Guid ColumnId, int NewPosition);

/// <summary>Moves the column to the new position via <see cref="LemonDo.Domain.Boards.Entities.Board.ReorderColumn"/>.</summary>
public sealed class ReorderColumnCommandHandler(IBoardRepository repository, IUnitOfWork unitOfWork)
{
    /// <inheritdoc/>
    public async Task<Result<ColumnDto, DomainError>> HandleAsync(ReorderColumnCommand command, CancellationToken ct = default)
    {
        var board = await repository.GetByIdAsync(BoardId.From(command.BoardId), ct);
        if (board is null)
            return Result<ColumnDto, DomainError>.Failure(
                DomainError.NotFound("Board", command.BoardId.ToString()));

        var result = board.ReorderColumn(ColumnId.From(command.ColumnId), command.NewPosition);
        if (result.IsFailure)
            return Result<ColumnDto, DomainError>.Failure(result.Error);

        await repository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var column = board.Columns.First(c => c.Id == ColumnId.From(command.ColumnId));
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
