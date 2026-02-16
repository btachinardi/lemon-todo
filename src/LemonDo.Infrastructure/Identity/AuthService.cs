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
/// Anti-Corruption Layer: translates between the domain model and ASP.NET Identity + JWT.
/// Handles ONLY credentials and authorization — no user profile data.
/// User profile data is managed by <see cref="LemonDo.Domain.Identity.Repositories.IUserRepository"/>.
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
    public async Task<Result<DomainError>> CreateCredentialsAsync(
        UserId userId, string emailHash, string password, CancellationToken ct)
    {
        // Check for duplicate via email hash (UserName stores the hash)
        var existing = await userManager.FindByNameAsync(emailHash);
        if (existing is not null)
            return Result<DomainError>.Failure(
                DomainError.Conflict("auth", "Email already registered."));

        var appUser = new ApplicationUser
        {
            Id = userId.Value,
            UserName = emailHash,
        };

        var identityResult = await userManager.CreateAsync(appUser, password);
        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors.Select(e => e.Description).ToList();
            logger.LogWarning("Credential creation failed: {Errors}", string.Join(", ", errors));

            return Result<DomainError>.Failure(
                DomainError.Validation("auth", string.Join(" ", errors)));
        }

        // Assign "User" role
        if (await roleManager.RoleExistsAsync("User"))
            await userManager.AddToRoleAsync(appUser, "User");

        return Result<DomainError>.Success();
    }

    /// <inheritdoc />
    public async Task<Result<UserId, DomainError>> AuthenticateAsync(
        string email, string password, CancellationToken ct)
    {
        // Look up by email hash stored in UserName
        var emailHash = ProtectedDataHasher.HashEmail(email);
        var user = await userManager.FindByNameAsync(emailHash);
        if (user is null)
            return Result<UserId, DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid email or password."));

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            logger.LogWarning("Account locked for {EmailHash}", LogHelpers.MaskEmail(email));
            return Result<UserId, DomainError>.Failure(
                DomainError.RateLimited("auth", "Account temporarily locked due to too many failed attempts. Please try again later."));
        }

        if (!signInResult.Succeeded)
            return Result<UserId, DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid email or password."));

        return Result<UserId, DomainError>.Success(UserId.Reconstruct(user.Id));
    }

    /// <inheritdoc />
    public async Task<AuthTokens> GenerateTokensAsync(UserId userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.Value.ToString())
            ?? throw new InvalidOperationException($"Identity user {userId} not found.");

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateAccessToken(user.Id, roles);
        var refreshToken = JwtTokenService.GenerateRefreshToken();

        await StoreRefreshTokenAsync(user.Id, refreshToken, ct);

        return new AuthTokens(accessToken, refreshToken, roles.AsReadOnly());
    }

    /// <inheritdoc />
    public async Task<Result<(UserId UserId, AuthTokens Tokens), DomainError>> RefreshTokenAsync(
        string refreshToken, CancellationToken ct)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (storedToken is null || !storedToken.IsActive)
            return Result<(UserId, AuthTokens), DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid or expired refresh token."));

        // Revoke old token
        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        var userId = UserId.Reconstruct(storedToken.UserId);
        var tokens = await GenerateTokensAsync(userId, ct);

        return Result<(UserId, AuthTokens), DomainError>.Success((userId, tokens));
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

    /// <inheritdoc />
    public async Task<Result<DomainError>> VerifyPasswordAsync(
        Guid userId, string password, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result<DomainError>.Failure(DomainError.NotFound("user", userId.ToString()));

        var isValid = await userManager.CheckPasswordAsync(user, password);
        if (!isValid)
            return Result<DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid password. Re-authentication failed."));

        return Result<DomainError>.Success();
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
