namespace LemonDo.Application.Extensions;

using LemonDo.Application.Boards.Commands;
using LemonDo.Application.Common;
using LemonDo.Application.Tasks.Commands;
using LemonDo.Application.Tasks.Queries;
using Microsoft.Extensions.DependencyInjection;

/// <summary>DI registration for the Application layer (command and query handlers).</summary>
public static class ApplicationServiceExtensions
{
    /// <summary>Registers all command and query handlers as scoped services.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Application metrics (singleton — counters are thread-safe)
        services.AddSingleton<ApplicationMetrics>();

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
