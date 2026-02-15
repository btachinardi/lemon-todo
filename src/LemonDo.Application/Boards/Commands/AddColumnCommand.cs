namespace LemonDo.Application.Boards.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Command to add a new column to a board with a target status mapping.</summary>
public sealed record AddColumnCommand(Guid BoardId, string Name, string TargetStatus, int? Position = null);

/// <summary>Validates the column name and status, then delegates to <see cref="LemonDo.Domain.Boards.Entities.Board.AddColumn"/>.</summary>
public sealed class AddColumnCommandHandler(IBoardRepository repository, IUnitOfWork unitOfWork)
{
    /// <inheritdoc/>
    public async Task<Result<ColumnDto, DomainError>> HandleAsync(AddColumnCommand command, CancellationToken ct = default)
    {
        var board = await repository.GetByIdAsync(BoardId.From(command.BoardId), ct);
        if (board is null)
            return Result<ColumnDto, DomainError>.Failure(
                DomainError.NotFound("Board", command.BoardId.ToString()));

        var nameResult = ColumnName.Create(command.Name);
        if (nameResult.IsFailure)
            return Result<ColumnDto, DomainError>.Failure(nameResult.Error);

        if (!Enum.TryParse<TaskStatus>(command.TargetStatus, true, out var targetStatus))
            return Result<ColumnDto, DomainError>.Failure(
                DomainError.Validation("targetStatus", $"Invalid target status: '{command.TargetStatus}'. Valid values: Todo, InProgress, Done."));

        var result = board.AddColumn(nameResult.Value, targetStatus, command.Position);
        if (result.IsFailure)
            return Result<ColumnDto, DomainError>.Failure(result.Error);

        await repository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var addedColumn = board.Columns.First(c => c.Name.Value == nameResult.Value.Value);
        return Result<ColumnDto, DomainError>.Success(new ColumnDto
        {
            Id = addedColumn.Id.Value,
            Name = addedColumn.Name.Value,
            TargetStatus = addedColumn.TargetStatus.ToString(),
            Position = addedColumn.Position,
            MaxTasks = addedColumn.MaxTasks
        });
    }
}
