namespace LemonDo.Api.Tests.Infrastructure;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Infrastructure.Identity;
using LemonDo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    public const string TestUserEmail = "test@lemondo.dev";
    public const string TestUserPassword = "TestPass123!";
    public const string TestUserDisplayName = "Test User";

    public const string AdminUserEmail = "admin@lemondo.dev";
    public const string AdminUserPassword = "AdminPass123!";
    public const string AdminUserDisplayName = "Admin User";

    public const string SystemAdminUserEmail = "sysadmin@lemondo.dev";
    public const string SystemAdminUserPassword = "SysAdminPass123!";
    public const string SystemAdminUserDisplayName = "System Admin";

    private static readonly Dictionary<string, string?> TestSettings = new()
    {
        ["Jwt:Issuer"] = "LemonDo",
        ["Jwt:Audience"] = "LemonDo",
        ["Jwt:SecretKey"] = "test-secret-key-at-least-32-characters-long!!",
        ["Jwt:AccessTokenExpirationMinutes"] = "60",
        ["Jwt:RefreshTokenExpirationDays"] = "7",
        ["RateLimiting:Auth:PermitLimit"] = "10000",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(TestSettings);
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LemonDoDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Create shared in-memory SQLite connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<LemonDoDbContext>(options =>
                options.UseSqlite(_connection)
                    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddHealthChecks();
        });

        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LemonDoDbContext>();
        db.Database.EnsureCreated();

        // Seed all roles
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        string[] roles = [Roles.User, Roles.Admin, Roles.SystemAdmin];
        foreach (var role in roles)
        {
            if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
                roleManager.CreateAsync(new IdentityRole<Guid>(role)).GetAwaiter().GetResult();
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Seed test user (regular User role)
        SeedUser(userManager, db, TestUserEmail, TestUserPassword, TestUserDisplayName, Roles.User);

        // Seed admin user
        SeedUser(userManager, db, AdminUserEmail, AdminUserPassword, AdminUserDisplayName, Roles.Admin);

        // Seed system admin user
        SeedUser(userManager, db, SystemAdminUserEmail, SystemAdminUserPassword, SystemAdminUserDisplayName, Roles.SystemAdmin);

        // Keep legacy default board for backward compat
        var legacyBoardRepo = scope.ServiceProvider.GetRequiredService<IBoardRepository>();
        var existing = legacyBoardRepo.GetDefaultForUserAsync(UserId.Default).GetAwaiter().GetResult();
        if (existing is null)
        {
            var result = Board.CreateDefault(UserId.Default);
            legacyBoardRepo.AddAsync(result.Value).GetAwaiter().GetResult();
            db.SaveChanges();
        }

        return host;
    }

    private static void SeedUser(
        UserManager<ApplicationUser> userManager,
        LemonDoDbContext db,
        string email,
        string password,
        string displayName,
        string role)
    {
        var existingUser = userManager.FindByEmailAsync(email).GetAwaiter().GetResult();
        if (existingUser is not null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
        };
        userManager.CreateAsync(user, password).GetAwaiter().GetResult();
        userManager.AddToRoleAsync(user, role).GetAwaiter().GetResult();

        // Create default board for user
        var userId = new UserId(user.Id);
        var boardResult = Board.CreateDefault(userId);
        if (boardResult.IsSuccess)
        {
            var boardRepo = db.Set<Board>();
            boardRepo.Add(boardResult.Value);
            db.SaveChanges();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}
