namespace LemonDo.Infrastructure.Identity;

using System.Security.Cryptography;
using System.Text;
using LemonDo.Application.Common;
using LemonDo.Application.Identity;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Anti-Corruption Layer implementation that translates between the domain model
/// and ASP.NET Identity + JWT infrastructure.
/// </summary>
public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    JwtTokenService tokenService,
    LemonDoDbContext dbContext,
    IOptions<JwtSettings> jwtSettings,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    /// <inheritdoc />
    public async Task<Result<AuthResult, DomainError>> RegisterAsync(
        UserId userId, Email email, string password, DisplayName displayName,
        CancellationToken ct)
    {
        var appUser = new ApplicationUser
        {
            Id = userId.Value,
            UserName = email.Value,
            Email = email.Value,
            DisplayName = displayName.Value,
        };

        var identityResult = await userManager.CreateAsync(appUser, password);
        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors.Select(e => e.Description).ToList();
            logger.LogWarning("Registration failed for {EmailHash}: {Errors}", LogHelpers.MaskEmail(email.Value), string.Join(", ", errors));

            if (identityResult.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
                return Result<AuthResult, DomainError>.Failure(
                    DomainError.Conflict("auth", "Email already registered."));

            return Result<AuthResult, DomainError>.Failure(
                DomainError.Validation("auth", string.Join(" ", errors)));
        }

        // Assign "User" role
        if (await roleManager.RoleExistsAsync("User"))
            await userManager.AddToRoleAsync(appUser, "User");

        return await GenerateAuthResultAsync(appUser, ct);
    }

    /// <inheritdoc />
    public async Task<Result<AuthResult, DomainError>> LoginAsync(
        string email, string password, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Result<AuthResult, DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid email or password."));

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            logger.LogWarning("Account locked for {EmailHash}", LogHelpers.MaskEmail(email));
            return Result<AuthResult, DomainError>.Failure(
                DomainError.RateLimited("auth", "Account temporarily locked due to too many failed attempts. Please try again later."));
        }

        if (!signInResult.Succeeded)
            return Result<AuthResult, DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid email or password."));

        return await GenerateAuthResultAsync(user, ct);
    }

    /// <inheritdoc />
    public async Task<Result<AuthResult, DomainError>> RefreshTokenAsync(
        string refreshToken, CancellationToken ct)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (storedToken is null || !storedToken.IsActive)
            return Result<AuthResult, DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid or expired refresh token."));

        // Revoke old token
        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        var user = await userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user is null)
            return Result<AuthResult, DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid or expired refresh token."));

        return await GenerateAuthResultAsync(user, ct);
    }

    /// <inheritdoc />
    public async Task<Result<DomainError>> RevokeRefreshTokenAsync(
        string refreshToken, CancellationToken ct)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (storedToken is not null)
        {
            storedToken.RevokedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(ct);
        }

        return Result<DomainError>.Success();
    }

    private async Task<Result<AuthResult, DomainError>> GenerateAuthResultAsync(
        ApplicationUser user, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email!, user.DisplayName, roles);
        var refreshToken = JwtTokenService.GenerateRefreshToken();

        await StoreRefreshTokenAsync(user.Id, refreshToken, ct);

        return Result<AuthResult, DomainError>.Success(new AuthResult(
            user.Id,
            user.Email!,
            user.DisplayName,
            roles.AsReadOnly(),
            accessToken,
            refreshToken));
    }

    private async Task StoreRefreshTokenAsync(Guid userId, string refreshToken, CancellationToken ct)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.RefreshTokens.Add(token);
        await dbContext.SaveChangesAsync(ct);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
