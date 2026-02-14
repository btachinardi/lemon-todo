namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    Priority Priority = Priority.None,
    DateTimeOffset? DueDate = null,
    IReadOnlyList<string>? Tags = null);

public sealed class CreateTaskCommandHandler(
    IBoardTaskRepository repository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<BoardTaskDto, DomainError>> HandleAsync(CreateTaskCommand command, CancellationToken ct = default)
    {
        var titleResult = TaskTitle.Create(command.Title);
        if (titleResult.IsFailure)
            return Result<BoardTaskDto, DomainError>.Failure(titleResult.Error);

        var descResult = TaskDescription.Create(command.Description);
        if (descResult.IsFailure)
            return Result<BoardTaskDto, DomainError>.Failure(descResult.Error);

        var tags = new List<Tag>();
        if (command.Tags is not null)
        {
            foreach (var tagStr in command.Tags)
            {
                var tagResult = Tag.Create(tagStr);
                if (tagResult.IsFailure)
                    return Result<BoardTaskDto, DomainError>.Failure(tagResult.Error);
                tags.Add(tagResult.Value);
            }
        }

        var taskResult = BoardTask.Create(UserId.Default, titleResult.Value, descResult.Value, command.Priority, command.DueDate, tags);
        if (taskResult.IsFailure)
            return Result<BoardTaskDto, DomainError>.Failure(taskResult.Error);

        var board = await boardRepository.GetDefaultForUserAsync(UserId.Default, ct);
        if (board is not null && board.Columns.Count > 0)
        {
            var firstColumn = board.Columns.OrderBy(c => c.Position).First();
            var existingTasks = await repository.GetByColumnAsync(firstColumn.Id, ct);
            taskResult.Value.MoveTo(firstColumn.Id, existingTasks.Count);
        }

        await repository.AddAsync(taskResult.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<BoardTaskDto, DomainError>.Success(BoardTaskDtoMapper.ToDto(taskResult.Value));
    }
}
