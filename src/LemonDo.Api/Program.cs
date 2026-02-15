using System.Reflection;
using System.Text;
using LemonDo.Api.Auth;
using LemonDo.Api.Endpoints;
using LemonDo.Api.Middleware;
using LemonDo.Application.Common;
using LemonDo.Application.Extensions;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Infrastructure.Extensions;
using LemonDo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<LemonDoDbContext>("database", tags: ["ready"]);

// JWT Authentication
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Deferred JWT bearer options — reads from IOptions<JwtSettings> at resolution time
// so test overrides via ConfigureAppConfiguration are included.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((bearerOptions, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.Zero,
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Auto-migrate on startup and seed default board
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("LemonDo.Api.Startup");

var version = typeof(LemonDo.Api.Endpoints.TaskEndpoints).Assembly
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
startupLogger.LogInformation("LemonDo API v{Version} starting", version);

try
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<LemonDoDbContext>();

    startupLogger.LogInformation("Applying database migrations...");
    await db.Database.MigrateAsync();
    startupLogger.LogInformation("Database migrations applied successfully");

    // Seed roles
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    string[] roles = ["User", "Admin"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            startupLogger.LogInformation("Seeded role {Role}", role);
        }
    }

    // Seed default board for legacy single-user mode (CP1 compatibility)
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

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapAuthEndpoints();
app.MapTaskEndpoints();
app.MapBoardEndpoints();

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();
