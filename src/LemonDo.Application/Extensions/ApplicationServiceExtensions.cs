namespace LemonDo.Application.Extensions;

using LemonDo.Application.Administration;
using LemonDo.Application.Administration.Commands;
using LemonDo.Application.Administration.EventHandlers;
using LemonDo.Application.Administration.Queries;
using LemonDo.Application.Boards.Commands;
using LemonDo.Application.Common;
using LemonDo.Application.Identity.Commands;
using LemonDo.Application.Tasks.Commands;
using LemonDo.Application.Tasks.Queries;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Events;
using LemonDo.Domain.Tasks.Events;
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

        // Identity command handlers
        services.AddScoped<RegisterUserCommandHandler>();
        services.AddScoped<LoginUserCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<RevokeRefreshTokenCommandHandler>();

        // Task query handlers
        services.AddScoped<GetTaskByIdQueryHandler>();
        services.AddScoped<ListTasksQueryHandler>();
        services.AddScoped<GetBoardQueryHandler>();
        services.AddScoped<GetDefaultBoardQueryHandler>();

        // Administration
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<SearchAuditLogQueryHandler>();
        services.AddScoped<ListUsersAdminQueryHandler>();
        services.AddScoped<GetUserAdminQueryHandler>();
        services.AddScoped<AssignRoleCommandHandler>();
        services.AddScoped<RemoveRoleCommandHandler>();
        services.AddScoped<DeactivateUserCommandHandler>();
        services.AddScoped<ReactivateUserCommandHandler>();
        services.AddScoped<RevealPiiCommandHandler>();

        // Audit event handlers
        services.AddScoped<IDomainEventHandler<UserRegisteredEvent>, AuditOnUserRegistered>();
        services.AddScoped<IDomainEventHandler<TaskCreatedEvent>, AuditOnTaskCreated>();
        services.AddScoped<IDomainEventHandler<TaskDeletedEvent>, AuditOnTaskDeleted>();
        services.AddScoped<IDomainEventHandler<TaskStatusChangedEvent>, AuditOnTaskStatusChanged>();

        return services;
    }
}
