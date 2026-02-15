namespace LemonDo.Application.Tasks.Queries;

using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;

public sealed record GetDefaultBoardQuery;

public sealed class GetDefaultBoardQueryHandler(IBoardRepository boardRepository, ITaskRepository taskRepository)
{
    public async Task<Result<BoardDto, DomainError>> HandleAsync(CancellationToken ct = default)
    {
        var board = await boardRepository.GetDefaultForUserAsync(UserId.Default, ct);
        if (board is null)
            return Result<BoardDto, DomainError>.Failure(
                DomainError.NotFound("Board", "default"));

        var activeTaskIds = await taskRepository.GetActiveTaskIdsAsync(UserId.Default, ct);
        return Result<BoardDto, DomainError>.Success(BoardDtoMapper.ToDto(board, activeTaskIds));
    }
}
