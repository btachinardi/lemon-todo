namespace LemonDo.Api.Endpoints;

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LemonDo.Api.Auth;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Infrastructure.Identity;
using LemonDo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>Minimal API endpoint definitions for authentication under <c>/api/auth</c>.</summary>
public static class AuthEndpoints
{
    private static readonly string LogCategory = "LemonDo.Api.Auth";

    /// <summary>Maps all authentication endpoints under <c>/api/auth</c>.</summary>
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register).AllowAnonymous();
        group.MapPost("/login", Login).AllowAnonymous();
        group.MapPost("/refresh", Refresh).AllowAnonymous();
        group.MapPost("/logout", Logout).RequireAuthorization();
        group.MapGet("/me", GetMe).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        JwtTokenService tokenService,
        IBoardRepository boardRepository,
        LemonDoDbContext dbContext,
        IOptions<JwtSettings> jwtSettings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(LogCategory);
        logger.LogInformation("Registering user {Email}", request.Email);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, string.Join(", ", errors));

            if (result.Errors.Any(e => e.Code == "DuplicateEmail" || e.Code == "DuplicateUserName"))
                return Results.Conflict(new { type = "auth.duplicate_email", title = "Email already registered.", status = 409 });

            return Results.BadRequest(new { type = "auth.validation", title = "Registration failed.", status = 400, errors });
        }

        // Assign "User" role
        if (await roleManager.RoleExistsAsync("User"))
            await userManager.AddToRoleAsync(user, "User");

        // Create default board for new user
        var userId = new UserId(user.Id);
        var boardResult = Board.CreateDefault(userId);
        if (boardResult.IsSuccess)
        {
            await boardRepository.AddAsync(boardResult.Value);
            await dbContext.SaveChangesAsync();
        }

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshToken = JwtTokenService.GenerateRefreshToken();

        await StoreRefreshTokenAsync(dbContext, user.Id, refreshToken, jwtSettings.Value.RefreshTokenExpirationDays);

        logger.LogInformation("User {UserId} registered successfully", user.Id);

        return Results.Ok(new AuthResponse(
            accessToken,
            refreshToken,
            new UserResponse(user.Id, user.Email!, user.DisplayName)));
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokenService,
        LemonDoDbContext dbContext,
        IOptions<JwtSettings> jwtSettings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(LogCategory);
        logger.LogInformation("Login attempt for {Email}", request.Email);

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            logger.LogWarning("Login failed for {Email}: invalid credentials", request.Email);
            return Results.Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshToken = JwtTokenService.GenerateRefreshToken();

        await StoreRefreshTokenAsync(dbContext, user.Id, refreshToken, jwtSettings.Value.RefreshTokenExpirationDays);

        logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return Results.Ok(new AuthResponse(
            accessToken,
            refreshToken,
            new UserResponse(user.Id, user.Email!, user.DisplayName)));
    }

    private static async Task<IResult> Refresh(
        RefreshRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokenService,
        LemonDoDbContext dbContext,
        IOptions<JwtSettings> jwtSettings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(LogCategory);

        var tokenHash = HashToken(request.RefreshToken);
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken is null || !storedToken.IsActive)
        {
            logger.LogWarning("Refresh token invalid or expired");
            return Results.Unauthorized();
        }

        // Revoke old token
        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        var user = await userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user is null)
            return Results.Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        var newAccessToken = tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = JwtTokenService.GenerateRefreshToken();

        await StoreRefreshTokenAsync(dbContext, user.Id, newRefreshToken, jwtSettings.Value.RefreshTokenExpirationDays);

        logger.LogInformation("Token refreshed for user {UserId}", user.Id);

        return Results.Ok(new AuthResponse(
            newAccessToken,
            newRefreshToken,
            new UserResponse(user.Id, user.Email!, user.DisplayName)));
    }

    private static async Task<IResult> Logout(
        [FromBody] RefreshRequest? request,
        ClaimsPrincipal user,
        LemonDoDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(LogCategory);

        if (request?.RefreshToken is not null)
        {
            var tokenHash = HashToken(request.RefreshToken);
            var storedToken = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

            if (storedToken is not null)
            {
                storedToken.RevokedAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync();
            }
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        logger.LogInformation("User {UserId} logged out", userId);

        return Results.Ok(new { message = "Logged out successfully." });
    }

    private static IResult GetMe(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = user.FindFirstValue(ClaimTypes.Email);
        var displayName = user.FindFirstValue("display_name");

        if (userId is null)
            return Results.Unauthorized();

        return Results.Ok(new UserResponse(Guid.Parse(userId), email ?? "", displayName ?? ""));
    }

    private static async Task StoreRefreshTokenAsync(LemonDoDbContext dbContext, Guid userId, string refreshToken, int expirationDays)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(expirationDays),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.RefreshTokens.Add(token);
        await dbContext.SaveChangesAsync();
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
