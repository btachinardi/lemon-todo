namespace LemonDo.Application.Tasks.Queries;

using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;

public sealed record GetDefaultBoardQuery;

public sealed class GetDefaultBoardQueryHandler(IBoardRepository repository)
{
    public async Task<Result<BoardDto, DomainError>> HandleAsync(CancellationToken ct = default)
    {
        var board = await repository.GetDefaultForUserAsync(UserId.Default, ct);
        if (board is null)
            return Result<BoardDto, DomainError>.Failure(
                DomainError.NotFound("Board", "default"));

        return Result<BoardDto, DomainError>.Success(BoardDtoMapper.ToDto(board));
    }
}
