namespace LemonDo.Application.Tasks.Queries;

using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>Query to retrieve a board by its ID.</summary>
public sealed record GetBoardQuery(Guid BoardId);

/// <summary>Loads the board with columns and cards, filtering out cards for deleted/archived tasks.</summary>
public sealed class GetBoardQueryHandler(IBoardRepository boardRepository, ITaskRepository taskRepository, ILogger<GetBoardQueryHandler> logger)
{
    /// <inheritdoc/>
    public async Task<Result<BoardDto, DomainError>> HandleAsync(GetBoardQuery query, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching board {BoardId}", query.BoardId);

        var board = await boardRepository.GetByIdAsync(BoardId.From(query.BoardId), ct);
        if (board is null)
        {
            logger.LogWarning("Board {BoardId} not found", query.BoardId);
            return Result<BoardDto, DomainError>.Failure(
                DomainError.NotFound("Board", query.BoardId.ToString()));
        }

        var activeTaskIds = await taskRepository.GetActiveTaskIdsAsync(UserId.Default, ct);
        logger.LogInformation("Board {BoardId} fetched successfully", query.BoardId);
        return Result<BoardDto, DomainError>.Success(BoardDtoMapper.ToDto(board, activeTaskIds));
    }
}
