namespace LemonDo.Application.Tasks.Queries;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>Query to retrieve the current user's default board.</summary>
public sealed record GetDefaultBoardQuery;

/// <summary>Loads the default board with columns and cards, filtering out cards for deleted/archived tasks.</summary>
public sealed class GetDefaultBoardQueryHandler(IBoardRepository boardRepository, ITaskRepository taskRepository, ICurrentUserService currentUser, ILogger<GetDefaultBoardQueryHandler> logger)
{
    /// <inheritdoc/>
    public async Task<Result<BoardDto, DomainError>> HandleAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Fetching default board");

        var board = await boardRepository.GetDefaultForUserAsync(currentUser.UserId, ct);
        if (board is null)
        {
            logger.LogWarning("Default board not found");
            return Result<BoardDto, DomainError>.Failure(
                DomainError.NotFound("Board", "default"));
        }

        var activeTaskIds = await taskRepository.GetActiveTaskIdsAsync(currentUser.UserId, ct);
        logger.LogInformation("Default board fetched successfully");
        return Result<BoardDto, DomainError>.Success(BoardDtoMapper.ToDto(board, activeTaskIds));
    }
}
