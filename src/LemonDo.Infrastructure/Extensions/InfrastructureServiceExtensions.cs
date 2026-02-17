namespace LemonDo.Infrastructure.Extensions;

using LemonDo.Application.Administration;
using LemonDo.Application.Administration.Commands;
using LemonDo.Application.Administration.Queries;
using LemonDo.Application.Common;
using LemonDo.Application.Identity;
using LemonDo.Infrastructure.Security;
using LemonDo.Domain.Administration.Repositories;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Identity.Repositories;
using LemonDo.Domain.Notifications.Repositories;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Infrastructure.Analytics;
using LemonDo.Infrastructure.Notifications;
using LemonDo.Infrastructure.Events;
using LemonDo.Infrastructure.Identity;
using LemonDo.Infrastructure.Persistence;
using LemonDo.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>DI registration for the Infrastructure layer (DbContext, repositories, event dispatcher).</summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>Registers <see cref="LemonDoDbContext"/>, repositories, and the domain event dispatcher.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Use the (sp, options) overload to defer provider selection until resolution time.
        // This allows WebApplicationFactory test overrides to change DatabaseProvider/ConnectionString
        // via ConfigureAppConfiguration before the provider is selected.
        services.AddDbContext<LemonDoDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connStr = config.GetConnectionString("DefaultConnection")
                ?? "Data Source=lemondo.db";
            var provider = config.GetValue<string>("DatabaseProvider") ?? "Sqlite";

            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                options.UseSqlServer(connStr, b => b.MigrationsAssembly("LemonDo.Migrations.SqlServer"));
            else
                options.UseSqlite(connStr, b => b.MigrationsAssembly("LemonDo.Migrations.Sqlite"));
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LemonDoDbContext>());
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // JWT token services
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();
        services.AddScoped<JwtTokenService>();

        // Identity + Auth ACL
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = false; // Uniqueness enforced via UserName (email hash)

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<LemonDoDbContext>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminUserQuery, AdminUserQuery>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddHostedService<RefreshTokenCleanupService>();

        // Analytics
        services.AddSingleton<IAnalyticsService, ConsoleAnalyticsService>();

        // Background services
        services.AddHostedService<DueDateReminderService>();

        // Field encryption for protected data at rest
        services.AddSingleton<IFieldEncryptionService, AesFieldEncryptionService>();

        // Audited protected data access service — the ONLY authorized path for decrypting protected data
        services.AddScoped<IProtectedDataAccessService, ProtectedDataAccessService>();

        return services;
    }
}
