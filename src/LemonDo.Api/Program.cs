using System.Reflection;
using System.Text;
using LemonDo.Api.Auth;
using LemonDo.Api.Endpoints;
using LemonDo.Api.Logging;
using LemonDo.Api.Middleware;
using LemonDo.Api.Services;
using LemonDo.Application.Common;
using LemonDo.Application.Extensions;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Application.Identity.Commands;
using LemonDo.Infrastructure.Extensions;
using LemonDo.Infrastructure.Identity;
using LemonDo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog — uses AddSerilog (not UseSerilog) to avoid static Log.Logger
// mutation that breaks WebApplicationFactory-based tests ("logger already frozen").
builder.Services.AddSerilog(configuration => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "LemonDo.Api")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .Enrich.WithProperty("MachineName", System.Environment.MachineName)
    .Enrich.With<ProtectedDataMaskingEnricher>()
    .Destructure.With<ProtectedDataDestructuringPolicy>());

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<LemonDoDbContext>("database", tags: ["ready"]);

// JWT Authentication
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IRequestContext, HttpRequestContext>();

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
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Roles.RequireAdminOrAbove, policy =>
        policy.RequireRole(Roles.Admin, Roles.SystemAdmin));
    options.AddPolicy(Roles.RequireSystemAdmin, policy =>
        policy.RequireRole(Roles.SystemAdmin));
});

// CORS — allow frontend origin with credentials for cookie-based auth
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["https://localhost:5173", "http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Rate limiting — uses AddPolicy with deferred IConfiguration read so that
// WebApplicationFactory config overrides (e.g., PermitLimit=10000) are applied.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
    {
        var config = context.RequestServices.GetRequiredService<IConfiguration>();
        var permitLimit = config.GetValue("RateLimiting:Auth:PermitLimit", 20);
        return RateLimitPartition.GetFixedWindowLimiter("auth", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

var app = builder.Build();

// Validate JWT settings eagerly at startup (triggers IValidateOptions)
_ = app.Services.GetRequiredService<IOptions<JwtSettings>>().Value;

// Auto-migrate on startup and seed default board
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("LemonDo.Api.Startup");

var version = typeof(TaskEndpoints).Assembly
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
    string[] roles = [Roles.User, Roles.Admin, Roles.SystemAdmin];
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

    // Seed development test accounts (one per role)
    if (app.Environment.IsDevelopment())
    {
        var registerHandler = scope.ServiceProvider.GetRequiredService<RegisterUserCommandHandler>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        (string Email, string Password, string DisplayName, string Role)[] devAccounts =
        [
            ("dev.user@lemondo.dev", "User1234", "Dev User", Roles.User),
            ("dev.admin@lemondo.dev", "Admin1234", "Dev Admin", Roles.Admin),
            ("dev.sysadmin@lemondo.dev", "SysAdmin1234", "Dev SysAdmin", Roles.SystemAdmin),
        ];

        foreach (var (email, password, displayName, role) in devAccounts)
        {
            // Check if user already exists via email hash
            var emailHash = ProtectedDataHasher.HashEmail(email);
            if (await userManager.FindByNameAsync(emailHash) is not null)
                continue;

            // RegisterUserCommandHandler creates credentials + domain User + board (via event)
            var command = new RegisterUserCommand(email, password, displayName);
            var registerResult = await registerHandler.HandleAsync(command);

            if (registerResult.IsSuccess)
            {
                // Assign additional role beyond "User" if needed
                if (role != Roles.User)
                {
                    var appUser = await userManager.FindByNameAsync(emailHash);
                    if (appUser is not null)
                        await userManager.AddToRoleAsync(appUser, role);
                }

                startupLogger.LogInformation(
                    "Seeded dev account with role {Role}", role);
            }
            else
            {
                startupLogger.LogWarning(
                    "Failed to seed dev account: {Error}", registerResult.Error.Code);
            }
        }
    }
}
catch (Exception ex)
{
    startupLogger.LogCritical(ex, "Failed to migrate or seed the database");
    throw;
}

// Middleware pipeline order:
// 1. Serilog request logging (HTTP-level structured logs)
// 2. Security headers (early — adds headers on every response)
// 3. Correlation ID (request tracing via Serilog LogContext)
// 4. Error handling (catches exceptions from downstream)
// 5. HSTS + HTTPS redirect (non-dev only)
// 6. CORS (credentials support for cookies)
// 7. Rate limiter (auth endpoint protection)
// 8. Authentication (JWT validation)
// 9. Authorization (route protection)
app.UseSerilogRequestLogging();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapAuthEndpoints();
app.MapTaskEndpoints();
app.MapBoardEndpoints();
app.MapAdminEndpoints();

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();
