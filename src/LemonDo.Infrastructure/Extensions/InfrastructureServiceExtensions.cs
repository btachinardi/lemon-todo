namespace LemonDo.Infrastructure.Extensions;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Infrastructure.Events;
using LemonDo.Infrastructure.Persistence;
using LemonDo.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>DI registration for the Infrastructure layer (DbContext, repositories, event dispatcher).</summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>Registers <see cref="LemonDoDbContext"/>, repositories, and the domain event dispatcher.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LemonDoDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=lemondo.db"));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LemonDoDbContext>());
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();

        return services;
    }
}
