namespace LemonDo.Api.Tests.Infrastructure;

using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
                options.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Disable Aspire service defaults for tests
        builder.ConfigureServices(services =>
        {
            // Remove HealthChecks that depend on Aspire infrastructure
            services.AddHealthChecks();
        });

        var host = base.CreateHost(builder);

        // Ensure database is created and seed default board
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LemonDoDbContext>();
        db.Database.EnsureCreated();

        var boardRepo = scope.ServiceProvider.GetRequiredService<IBoardRepository>();
        var existing = boardRepo.GetDefaultForUserAsync(UserId.Default).GetAwaiter().GetResult();
        if (existing is null)
        {
            var result = Board.CreateDefault(UserId.Default);
            boardRepo.AddAsync(result.Value).GetAwaiter().GetResult();
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
