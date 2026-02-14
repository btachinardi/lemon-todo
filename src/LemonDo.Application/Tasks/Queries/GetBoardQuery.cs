namespace LemonDo.Application.Tasks.Queries;

using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record GetBoardQuery(Guid BoardId);

public sealed class GetBoardQueryHandler(IBoardRepository repository)
{
    public async Task<Result<BoardDto, DomainError>> HandleAsync(GetBoardQuery query, CancellationToken ct = default)
    {
        var board = await repository.GetByIdAsync(BoardId.From(query.BoardId), ct);
        if (board is null)
            return Result<BoardDto, DomainError>.Failure(
                DomainError.NotFound("Board", query.BoardId.ToString()));

        return Result<BoardDto, DomainError>.Success(TaskItemDtoMapper.ToDto(board));
    }
}
