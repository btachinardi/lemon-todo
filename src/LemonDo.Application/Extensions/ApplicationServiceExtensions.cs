namespace LemonDo.Application.Extensions;

using LemonDo.Application.Boards.Commands;
using LemonDo.Application.Tasks.Commands;
using LemonDo.Application.Tasks.Queries;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Task command handlers
        services.AddScoped<CreateTaskCommandHandler>();
        services.AddScoped<UpdateTaskCommandHandler>();
        services.AddScoped<CompleteTaskCommandHandler>();
        services.AddScoped<UncompleteTaskCommandHandler>();
        services.AddScoped<DeleteTaskCommandHandler>();
        services.AddScoped<MoveTaskCommandHandler>();
        services.AddScoped<AddTagToTaskCommandHandler>();
        services.AddScoped<RemoveTagFromTaskCommandHandler>();
        services.AddScoped<ArchiveTaskCommandHandler>();
        services.AddScoped<BulkCompleteTasksCommandHandler>();

        // Board command handlers
        services.AddScoped<AddColumnCommandHandler>();
        services.AddScoped<RenameColumnCommandHandler>();
        services.AddScoped<RemoveColumnCommandHandler>();
        services.AddScoped<ReorderColumnCommandHandler>();

        // Task query handlers
        services.AddScoped<GetTaskByIdQueryHandler>();
        services.AddScoped<ListTasksQueryHandler>();
        services.AddScoped<GetBoardQueryHandler>();
        services.AddScoped<GetDefaultBoardQueryHandler>();

        return services;
    }
}
