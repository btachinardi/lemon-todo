namespace LemonDo.Api.Tests.Infrastructure;

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

    private static readonly Dictionary<string, string?> TestJwtSettings = new()
    {
        ["Jwt:Issuer"] = "LemonDo",
        ["Jwt:Audience"] = "LemonDo",
        ["Jwt:SecretKey"] = "test-secret-key-at-least-32-characters-long!!",
        ["Jwt:AccessTokenExpirationMinutes"] = "60",
        ["Jwt:RefreshTokenExpirationDays"] = "7",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(TestJwtSettings);
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

        // Seed roles
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (!roleManager.RoleExistsAsync("User").GetAwaiter().GetResult())
            roleManager.CreateAsync(new IdentityRole<Guid>("User")).GetAwaiter().GetResult();

        // Seed test user
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existingUser = userManager.FindByEmailAsync(TestUserEmail).GetAwaiter().GetResult();
        if (existingUser is null)
        {
            var user = new ApplicationUser
            {
                UserName = TestUserEmail,
                Email = TestUserEmail,
                DisplayName = TestUserDisplayName,
            };
            userManager.CreateAsync(user, TestUserPassword).GetAwaiter().GetResult();
            userManager.AddToRoleAsync(user, "User").GetAwaiter().GetResult();

            // Create default board for test user
            var userId = new UserId(user.Id);
            var boardResult = Board.CreateDefault(userId);
            if (boardResult.IsSuccess)
            {
                var boardRepo = scope.ServiceProvider.GetRequiredService<IBoardRepository>();
                boardRepo.AddAsync(boardResult.Value).GetAwaiter().GetResult();
                db.SaveChanges();
            }
        }

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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}
