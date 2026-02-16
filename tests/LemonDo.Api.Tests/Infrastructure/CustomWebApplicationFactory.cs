namespace LemonDo.Api.Tests.Infrastructure;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Entities;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Infrastructure.Identity;
using LemonDo.Infrastructure.Persistence;
using LemonDo.Infrastructure.Security;
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
        ["Encryption:FieldEncryptionKey"] = "dGVzdC1maWVsZC1lbmNyeXB0aW9uLWtleS0zMmJ5dGU=", // 32-byte test key
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
        var encryptionService = scope.ServiceProvider.GetRequiredService<IFieldEncryptionService>();

        // Seed test user (regular User role)
        SeedUser(userManager, encryptionService, db, TestUserEmail, TestUserPassword, TestUserDisplayName, Roles.User);

        // Seed admin user
        SeedUser(userManager, encryptionService, db, AdminUserEmail, AdminUserPassword, AdminUserDisplayName, Roles.Admin);

        // Seed system admin user
        SeedUser(userManager, encryptionService, db, SystemAdminUserEmail, SystemAdminUserPassword, SystemAdminUserDisplayName, Roles.SystemAdmin);

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
        IFieldEncryptionService encryptionService,
        LemonDoDbContext db,
        string email,
        string password,
        string displayName,
        string role)
    {
        var emailHash = ProtectedDataHasher.HashEmail(email);
        var existingUser = userManager.FindByNameAsync(emailHash).GetAwaiter().GetResult();
        if (existingUser is not null) return;

        var emailVo = Email.Create(email).Value;
        var displayNameVo = DisplayName.Create(displayName).Value;

        // 1. Create domain User (generates the shared ID + UserRegisteredEvent)
        var domainUser = User.Create(emailVo, displayNameVo).Value;

        // 2. Create Identity credentials with the SAME ID
        var appUser = new ApplicationUser
        {
            Id = domainUser.Id.Value,
            UserName = emailHash,
        };
        userManager.CreateAsync(appUser, password).GetAwaiter().GetResult();
        userManager.AddToRoleAsync(appUser, Roles.User).GetAwaiter().GetResult();
        if (role != Roles.User)
            userManager.AddToRoleAsync(appUser, role).GetAwaiter().GetResult();

        // 3. Persist domain User with encrypted shadow properties
        db.Users.Add(domainUser);
        var entry = db.Entry(domainUser);
        entry.Property("EmailHash").CurrentValue = emailHash;
        entry.Property("EncryptedEmail").CurrentValue = encryptionService.Encrypt(email);
        entry.Property("EncryptedDisplayName").CurrentValue = encryptionService.Encrypt(displayName);

        // SaveChangesAsync dispatches UserRegisteredEvent → CreateDefaultBoardOnUserRegistered
        db.SaveChangesAsync().GetAwaiter().GetResult();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}
