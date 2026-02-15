using LemonDo.Api.Endpoints;
using LemonDo.Api.Middleware;
using LemonDo.Application.Extensions;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Infrastructure.Extensions;
using LemonDo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<LemonDoDbContext>("database", tags: ["ready"]);

var app = builder.Build();

// Auto-migrate on startup and seed default board
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("LemonDo.Api.Startup");

try
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<LemonDoDbContext>();

    startupLogger.LogInformation("Applying database migrations...");
    await db.Database.MigrateAsync();
    startupLogger.LogInformation("Database migrations applied successfully");

    var boardRepo = scope.ServiceProvider.GetRequiredService<IBoardRepository>();
    var existing = await boardRepo.GetDefaultForUserAsync(UserId.Default);
    if (existing is null)
    {
        startupLogger.LogInformation("Seeding default board for user {UserId}", UserId.Default);
        var result = Board.CreateDefault(UserId.Default);
        await boardRepo.AddAsync(result.Value);
        await db.SaveChangesAsync();
        startupLogger.LogInformation("Default board seeded successfully");
    }
    else
    {
        startupLogger.LogDebug("Default board already exists, skipping seed");
    }
}
catch (Exception ex)
{
    startupLogger.LogCritical(ex, "Failed to migrate or seed the database");
    throw;
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapDefaultEndpoints();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapTaskEndpoints();
app.MapBoardEndpoints();

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();
