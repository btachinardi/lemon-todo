namespace LemonDo.Api.Endpoints;

using System.Security.Claims;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Extensions;
using LemonDo.Application.Identity;
using LemonDo.Application.Identity.Commands;
using LemonDo.Domain.Identity.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Infrastructure.Identity;
using Microsoft.Extensions.Options;

/// <summary>Minimal API endpoint definitions for authentication under <c>/api/auth</c>.</summary>
public static class AuthEndpoints
{
    private const string RefreshTokenCookieName = "refresh_token";

    /// <summary>
    /// Maps all authentication endpoints under <c>/api/auth</c> including register, login,
    /// token refresh, logout, and current user retrieval.
    /// </summary>
    /// <returns>The route group builder for method chaining.</returns>
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireRateLimiting("auth");

        group.MapPost("/register", Register).AllowAnonymous().Produces<AuthResponse>();
        group.MapPost("/login", Login).AllowAnonymous().Produces<AuthResponse>();
        group.MapPost("/refresh", Refresh).AllowAnonymous().Produces<AuthResponse>();
        group.MapPost("/logout", Logout).RequireAuthorization();
        group.MapGet("/me", GetMe).RequireAuthorization().Produces<UserResponse>();
        group.MapPost("/reveal-profile", RevealProfile).RequireAuthorization().Produces<RevealedProfileResponse>();

        return group;
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        RegisterUserCommandHandler handler,
        HttpContext httpContext,
        IOptions<JwtSettings> jwtSettings,
        CancellationToken ct)
    {
        var command = new RegisterUserCommand(request.Email!, request.Password!, request.DisplayName!);
        var result = await handler.HandleAsync(command, ct);

        return result.ToHttpResult(
            auth => SetCookieAndReturnResponse(httpContext, auth, jwtSettings.Value),
            httpContext: httpContext);
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        LoginUserCommandHandler handler,
        HttpContext httpContext,
        IOptions<JwtSettings> jwtSettings,
        CancellationToken ct)
    {
        var command = new LoginUserCommand(request.Email!, request.Password!);
        var result = await handler.HandleAsync(command, ct);

        return result.ToHttpResult(
            auth => SetCookieAndReturnResponse(httpContext, auth, jwtSettings.Value),
            httpContext: httpContext);
    }

    private static async Task<IResult> Refresh(
        RefreshTokenCommandHandler handler,
        HttpContext httpContext,
        IOptions<JwtSettings> jwtSettings,
        CancellationToken ct)
    {
        var refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            return Results.Unauthorized();

        var command = new RefreshTokenCommand(refreshToken);
        var result = await handler.HandleAsync(command, ct);

        return result.ToHttpResult(
            auth => SetCookieAndReturnResponse(httpContext, auth, jwtSettings.Value),
            httpContext: httpContext);
    }

    private static async Task<IResult> Logout(
        ClaimsPrincipal user,
        RevokeRefreshTokenCommandHandler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName];
        var command = new RevokeRefreshTokenCommand(refreshToken);
        var result = await handler.HandleAsync(command, ct);

        ClearRefreshTokenCookie(httpContext);

        return result.ToHttpResult(
            () => Results.Ok(new { message = "Logged out successfully." }),
            httpContext: httpContext);
    }

    private static async Task<IResult> GetMe(
        ClaimsPrincipal principal,
        IUserRepository userRepository)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr is null || !Guid.TryParse(userIdStr, out var guid))
            return Results.Unauthorized();

        var user = await userRepository.GetByIdAsync(UserId.Reconstruct(guid));
        if (user is null)
            return Results.Unauthorized();

        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        // Returns redacted values from domain User entity
        return Results.Ok(new UserResponse(user.Id.Value, user.RedactedEmail, user.RedactedDisplayName, roles));
    }

    private static async Task<IResult> RevealProfile(
        RevealOwnProfileCommandHandler handler,
        RevealProfileRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var command = new RevealOwnProfileCommand(request.Password!);
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult(
            data => Results.Ok(new RevealedProfileResponse(data.Email, data.DisplayName)),
            httpContext: httpContext);
    }

    private static IResult SetCookieAndReturnResponse(HttpContext httpContext, AuthResult auth, JwtSettings jwt)
    {
        SetRefreshTokenCookie(httpContext, auth.RefreshToken, jwt.RefreshTokenExpirationDays);

        return Results.Ok(new AuthResponse(
            auth.AccessToken,
            new UserResponse(auth.UserId, auth.RedactedEmail, auth.RedactedDisplayName, auth.Roles)));
    }

    private static void SetRefreshTokenCookie(HttpContext httpContext, string token, int expirationDays)
    {
        httpContext.Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !IsDevEnvironment(httpContext),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            MaxAge = TimeSpan.FromDays(expirationDays),
        });
    }

    private static void ClearRefreshTokenCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = !IsDevEnvironment(httpContext),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
        });
    }

    private static bool IsDevEnvironment(HttpContext httpContext)
    {
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        return env.IsDevelopment();
    }
}
