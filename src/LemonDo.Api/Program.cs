using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Http.Json;
using LemonDo.Api.Auth;
using LemonDo.Api.Endpoints;
using LemonDo.Api.Logging;
using LemonDo.Api.Middleware;
using LemonDo.Api.Serialization;
using LemonDo.Api.Services;
using LemonDo.Application.Common;
using LemonDo.Application.Extensions;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Application.Identity.Commands;
using LemonDo.Domain.Notifications.Enums;
using LemonDo.Domain.Tasks.ValueObjects;
using DomainTaskStatus = LemonDo.Domain.Tasks.ValueObjects.TaskStatus;
using LemonDo.Infrastructure.Extensions;
using LemonDo.Infrastructure.Identity;
using LemonDo.Infrastructure.Persistence;
using LemonDo.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Threading.RateLimiting;

// Build-time OpenAPI generation launches the app via GetDocument.Insider.
// Guard runtime-only behavior (Aspire telemetry, DB migration, seeding, middleware)
// behind this check. Service registration must stay unconditional so the OpenAPI
// generator can infer endpoint parameter sources.
var isBuildTimeDocGen = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

var builder = WebApplication.CreateBuilder(args);

if (!isBuildTimeDocGen)
{
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
}

builder.Services.AddOpenApi(options =>
{
    // Enrich string-typed DTO properties with enum constraints.
    // Priority, TaskStatus, and NotificationType are serialized via .ToString() in mappers,
    // so the OpenAPI generator sees "string" — this transformer adds the enum values.
    options.AddDocumentTransformer((document, _, cancellationToken) =>
    {
        var priorityValues = Enum.GetNames<Priority>().Select(v => (JsonNode)JsonValue.Create(v)!).ToList();
        var statusValues = Enum.GetNames<DomainTaskStatus>().Select(v => (JsonNode)JsonValue.Create(v)!).ToList();
        var notificationTypeValues = Enum.GetNames<NotificationType>().Select(v => (JsonNode)JsonValue.Create(v)!).ToList();

        if (document.Components?.Schemas is null) return Task.CompletedTask;

        foreach (var (name, schema) in document.Components.Schemas)
        {
            if (schema.Properties is null) continue;

            // Cast through concrete OpenApiSchema to access the settable Enum property
            // (IOpenApiSchema.Enum is read-only in Microsoft.OpenApi 2.0).
            if (schema.Properties.TryGetValue("priority", out var priorityProp)
                && priorityProp is OpenApiSchema prioritySchema && prioritySchema.Enum is null)
                prioritySchema.Enum = priorityValues;

            if (schema.Properties.TryGetValue("status", out var statusProp)
                && statusProp is OpenApiSchema statusSchema && statusSchema.Enum is null)
                statusSchema.Enum = statusValues;

            if (schema.Properties.TryGetValue("targetStatus", out var tsProp)
                && tsProp is OpenApiSchema tsSchema && tsSchema.Enum is null)
                tsSchema.Enum = statusValues;

            if (name.Contains("Notification", StringComparison.OrdinalIgnoreCase)
                && schema.Properties.TryGetValue("type", out var typeProp)
                && typeProp is OpenApiSchema typeSchema && typeSchema.Enum is null)
                typeSchema.Enum = notificationTypeValues;
        }

        return Task.CompletedTask;
    });
});

// Register all services unconditionally — the OpenAPI build-time generator needs
// them to infer endpoint parameter sources. Only runtime *behavior* is guarded.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

if (!isBuildTimeDocGen)
    builder.Services.AddSingleton<IConfigureOptions<JsonOptions>, ProtectedDataJsonConfigurator>();

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

if (!isBuildTimeDocGen)
{
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

        // Seed demo accounts when feature flag is enabled
        var enableDemoAccounts = app.Configuration.GetValue<bool>("Features:EnableDemoAccounts");
        if (enableDemoAccounts)
        {
            var registerHandler = scope.ServiceProvider.GetRequiredService<RegisterUserCommandHandler>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var encryptionService = scope.ServiceProvider.GetRequiredService<IFieldEncryptionService>();

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

                // Create EncryptedFields programmatically for seeding (bypassing JSON deserialization)
                var encryptedEmail = EncryptedEmailConverter.Create(email, encryptionService);
                var encryptedDisplayName = EncryptedDisplayNameConverter.Create(displayName, encryptionService);
                var protectedPassword = new ProtectedValue(password);

                // RegisterUserCommandHandler creates credentials + domain User + board (via event)
                var command = new RegisterUserCommand(encryptedEmail, protectedPassword, encryptedDisplayName);
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
}

app.MapOpenApi();
app.MapScalarApiReference();
app.MapAuthEndpoints();
app.MapTaskEndpoints();
app.MapBoardEndpoints();
app.MapAdminEndpoints();
app.MapAnalyticsEndpoints();
app.MapOnboardingEndpoints();
app.MapNotificationEndpoints();

app.MapGet("/api/config", (IConfiguration config) =>
    Results.Ok(new { EnableDemoAccounts = config.GetValue<bool>("Features:EnableDemoAccounts") }));

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();
