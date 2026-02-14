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

var app = builder.Build();

// Auto-migrate on startup and seed default board
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LemonDoDbContext>();
    await db.Database.MigrateAsync();

    var boardRepo = scope.ServiceProvider.GetRequiredService<IBoardRepository>();
    var existing = await boardRepo.GetDefaultForUserAsync(UserId.Default);
    if (existing is null)
    {
        var result = Board.CreateDefault(UserId.Default);
        await boardRepo.AddAsync(result.Value);
        await db.SaveChangesAsync();
    }
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapDefaultEndpoints();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapTaskEndpoints();
app.MapBoardEndpoints();

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();
