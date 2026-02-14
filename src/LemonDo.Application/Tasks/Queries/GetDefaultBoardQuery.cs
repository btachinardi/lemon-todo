namespace LemonDo.Application.Tasks.Queries;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.Repositories;

public sealed record GetDefaultBoardQuery;

public sealed class GetDefaultBoardQueryHandler(IBoardRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<BoardDto, DomainError>> HandleAsync(CancellationToken ct = default)
    {
        var board = await repository.GetDefaultForUserAsync(UserId.Default, ct);
        if (board is not null)
            return Result<BoardDto, DomainError>.Success(TaskItemDtoMapper.ToDto(board));

        var createResult = Board.CreateDefault(UserId.Default);
        if (createResult.IsFailure)
            return Result<BoardDto, DomainError>.Failure(createResult.Error);

        await repository.AddAsync(createResult.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<BoardDto, DomainError>.Success(TaskItemDtoMapper.ToDto(createResult.Value));
    }
}
