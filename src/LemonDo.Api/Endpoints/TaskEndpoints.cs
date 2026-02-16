namespace LemonDo.Api.Endpoints;

using LemonDo.Api.Contracts;
using LemonDo.Api.Extensions;
using LemonDo.Application.Common;
using LemonDo.Application.Tasks.Commands;
using LemonDo.Application.Tasks.Queries;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Minimal API endpoint definitions for the <c>/api/tasks</c> route group.</summary>
public static class TaskEndpoints
{
    /// <summary>
    /// Maps all task endpoints under <c>/api/tasks</c> including CRUD operations, lifecycle
    /// transitions (complete, uncomplete, archive), spatial positioning (move), tag management,
    /// sensitive note viewing, and bulk operations.
    /// </summary>
    /// <returns>The route group builder for method chaining.</returns>
    public static RouteGroupBuilder MapTaskEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks").WithTags("Tasks").RequireAuthorization();

        group.MapGet("/", ListTasks);
        group.MapPost("/", CreateTask);
        group.MapGet("/{id:guid}", GetTaskById);
        group.MapPut("/{id:guid}", UpdateTask);
        group.MapDelete("/{id:guid}", DeleteTask);
        group.MapPost("/{id:guid}/complete", CompleteTask);
        group.MapPost("/{id:guid}/uncomplete", UncompleteTask);
        group.MapPost("/{id:guid}/archive", ArchiveTask);
        group.MapPost("/{id:guid}/move", MoveTask);
        group.MapPost("/{id:guid}/tags", AddTag);
        group.MapDelete("/{id:guid}/tags/{tag}", RemoveTag);
        group.MapPost("/{id:guid}/view-note", ViewTaskNote);
        group.MapPost("/bulk/complete", BulkComplete);

        return group;
    }

    private static async Task<IResult> ListTasks(
        ListTasksQueryHandler handler,
        string? status = null,
        string? priority = null,
        string? search = null,
        string? tag = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var filter = new TaskListFilter
        {
            Status = Enum.TryParse<TaskStatus>(status, true, out var s) ? s : null,
            Priority = Enum.TryParse<Priority>(priority, true, out var p) ? p : null,
            SearchTerm = search,
            Tag = tag,
            Page = page,
            PageSize = pageSize
        };

        var result = await handler.HandleAsync(new ListTasksQuery(filter), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateTask(
        CreateTaskCommandHandler handler,
        CreateTaskRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!Enum.TryParse<Priority>(request.Priority, true, out var priority))
            priority = Priority.None;

        var command = new CreateTaskCommand(
            request.Title,
            request.Description,
            priority,
            request.DueDate,
            request.Tags,
            request.SensitiveNote);

        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(dto => Results.Created($"/api/tasks/{dto.Id}", dto), httpContext: httpContext);
    }

    private static async Task<IResult> GetTaskById(
        GetTaskByIdQueryHandler handler,
        Guid id,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetTaskByIdQuery(id), ct);
        return result.ToHttpResult(httpContext: httpContext);
    }

    private static async Task<IResult> UpdateTask(
        UpdateTaskCommandHandler handler,
        Guid id,
        UpdateTaskRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        Priority? priority = null;
        if (request.Priority is not null && Enum.TryParse<Priority>(request.Priority, true, out var p))
            priority = p;

        var command = new UpdateTaskCommand(
            id,
            request.Title,
            request.Description,
            priority,
            request.DueDate,
            request.ClearDueDate,
            request.SensitiveNote,
            request.ClearSensitiveNote);

        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(httpContext: httpContext);
    }

    private static async Task<IResult> DeleteTask(
        DeleteTaskCommandHandler handler,
        Guid id,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new DeleteTaskCommand(id), ct);
        return result.ToHttpResult(() => Results.Ok(new { Success = true }), httpContext: httpContext);
    }

    private static async Task<IResult> CompleteTask(
        CompleteTaskCommandHandler handler,
        Guid id,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new CompleteTaskCommand(id), ct);
        return result.ToHttpResult(() => Results.Ok(new { Id = id, Status = "Done" }), httpContext: httpContext);
    }

    private static async Task<IResult> UncompleteTask(
        UncompleteTaskCommandHandler handler,
        Guid id,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new UncompleteTaskCommand(id), ct);
        return result.ToHttpResult(() => Results.Ok(new { Id = id, Status = "Todo" }), httpContext: httpContext);
    }

    private static async Task<IResult> ArchiveTask(
        ArchiveTaskCommandHandler handler,
        Guid id,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ArchiveTaskCommand(id), ct);
        return result.ToHttpResult(() => Results.Ok(new { Id = id, IsArchived = true }), httpContext: httpContext);
    }

    private static async Task<IResult> MoveTask(
        MoveTaskCommandHandler handler,
        Guid id,
        MoveTaskRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var command = new MoveTaskCommand(id, request.ColumnId, request.PreviousTaskId, request.NextTaskId);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(() => Results.Ok(new { Id = id, request.ColumnId }), httpContext: httpContext);
    }

    private static async Task<IResult> AddTag(
        AddTagToTaskCommandHandler handler,
        Guid id,
        AddTagRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var command = new AddTagToTaskCommand(id, request.Tag);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(() => Results.Ok(new { Id = id }), httpContext: httpContext);
    }

    private static async Task<IResult> RemoveTag(
        RemoveTagFromTaskCommandHandler handler,
        Guid id,
        string tag,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var command = new RemoveTagFromTaskCommand(id, tag);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(() => Results.Ok(new { Id = id }), httpContext: httpContext);
    }

    private static async Task<IResult> ViewTaskNote(
        ViewTaskNoteCommandHandler handler,
        Guid id,
        ViewTaskNoteRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var command = new ViewTaskNoteCommand(id, request.Password);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(note => Results.Ok(new { Note = note }), httpContext: httpContext);
    }

    private static async Task<IResult> BulkComplete(
        BulkCompleteTasksCommandHandler handler,
        BulkCompleteRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var command = new BulkCompleteTasksCommand(request.TaskIds);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(() => Results.Ok(new { CompletedCount = request.TaskIds.Count, FailedCount = 0 }), httpContext: httpContext);
    }
}
