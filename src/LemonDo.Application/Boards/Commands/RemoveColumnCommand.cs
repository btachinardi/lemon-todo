namespace LemonDo.Application.Boards.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;

public sealed record RemoveColumnCommand(Guid BoardId, Guid ColumnId);

public sealed class RemoveColumnCommandHandler(IBoardRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<DomainError>> HandleAsync(RemoveColumnCommand command, CancellationToken ct = default)
    {
        var board = await repository.GetByIdAsync(BoardId.From(command.BoardId), ct);
        if (board is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Board", command.BoardId.ToString()));

        var result = board.RemoveColumn(ColumnId.From(command.ColumnId));
        if (result.IsFailure)
            return result;

        await repository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<DomainError>.Success();
    }
}
