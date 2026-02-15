namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Command to create a new task with optional description, priority, due date, and tags.</summary>
public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    Priority Priority = Priority.None,
    DateTimeOffset? DueDate = null,
    IReadOnlyList<string>? Tags = null);

/// <summary>
/// Creates a task and places it on the user's default board in the initial (Todo) column.
/// Coordinates both the Task and Board bounded contexts in a single unit of work.
/// </summary>
public sealed class CreateTaskCommandHandler(
    ITaskRepository taskRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<TaskDto, DomainError>> HandleAsync(CreateTaskCommand command, CancellationToken ct = default)
    {
        var titleResult = TaskTitle.Create(command.Title);
        if (titleResult.IsFailure)
            return Result<TaskDto, DomainError>.Failure(titleResult.Error);

        var descResult = TaskDescription.Create(command.Description);
        if (descResult.IsFailure)
            return Result<TaskDto, DomainError>.Failure(descResult.Error);

        var tags = new List<Tag>();
        if (command.Tags is not null)
        {
            foreach (var tagStr in command.Tags)
            {
                var tagResult = Tag.Create(tagStr);
                if (tagResult.IsFailure)
                    return Result<TaskDto, DomainError>.Failure(tagResult.Error);
                tags.Add(tagResult.Value);
            }
        }

        // Create task (defaults to Todo status)
        var taskResult = TaskEntity.Create(
            UserId.Default,
            titleResult.Value,
            descResult.Value,
            command.Priority,
            command.DueDate,
            tags);

        if (taskResult.IsFailure)
            return Result<TaskDto, DomainError>.Failure(taskResult.Error);

        var task = taskResult.Value;

        // Place on default board's initial column
        var board = await boardRepository.GetDefaultForUserAsync(UserId.Default, ct);
        if (board is null)
            return Result<TaskDto, DomainError>.Failure(
                DomainError.NotFound("Board", "default"));

        var initialColumn = board.GetInitialColumn();

        var placeResult = board.PlaceTask(task.Id, initialColumn.Id);
        if (placeResult.IsFailure)
            return Result<TaskDto, DomainError>.Failure(placeResult.Error);

        await taskRepository.AddAsync(task, ct);
        await boardRepository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<TaskDto, DomainError>.Success(TaskDtoMapper.ToDto(task));
    }
}
