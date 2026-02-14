namespace LemonDo.Infrastructure.Extensions;

using LemonDo.Application.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Infrastructure.Persistence;
using LemonDo.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LemonDoDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=lemondo.db"));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LemonDoDbContext>());
        services.AddScoped<ITaskItemRepository, TaskItemRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();

        return services;
    }
}
