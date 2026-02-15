namespace LemonDo.Application.Boards.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;

/// <summary>Command to rename a column on a board.</summary>
public sealed record RenameColumnCommand(Guid BoardId, Guid ColumnId, string Name);

/// <summary>Validates the new name and delegates to <see cref="LemonDo.Domain.Boards.Entities.Board.RenameColumn"/>.</summary>
public sealed class RenameColumnCommandHandler(IBoardRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<ColumnDto, DomainError>> HandleAsync(RenameColumnCommand command, CancellationToken ct = default)
    {
        var board = await repository.GetByIdAsync(BoardId.From(command.BoardId), ct);
        if (board is null)
            return Result<ColumnDto, DomainError>.Failure(
                DomainError.NotFound("Board", command.BoardId.ToString()));

        var nameResult = ColumnName.Create(command.Name);
        if (nameResult.IsFailure)
            return Result<ColumnDto, DomainError>.Failure(nameResult.Error);

        var result = board.RenameColumn(ColumnId.From(command.ColumnId), nameResult.Value);
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
