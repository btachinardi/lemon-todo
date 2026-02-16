namespace LemonDo.Application.Identity;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;

/// <summary>
/// Anti-Corruption Layer port for credential operations.
/// Handles ONLY authentication and authorization — no user profile data.
/// User data is managed by <see cref="LemonDo.Domain.Identity.Repositories.IUserRepository"/>.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Creates Identity credentials (password hash, lockout config) and assigns the "User" role.
    /// Does NOT store any user profile data — that's the repository's job.
    /// </summary>
    Task<Result<DomainError>> CreateCredentialsAsync(
        UserId userId, string emailHash, string password,
        CancellationToken ct = default);

    /// <summary>
    /// Authenticates by email (hashed internally) and password.
    /// Returns the user ID on success, or an error on failure (wrong credentials, lockout).
    /// </summary>
    Task<Result<UserId, DomainError>> AuthenticateAsync(
        string email, string password,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a new JWT access token + refresh token for the given user.
    /// Loads roles from Identity. Stores the refresh token hash.
    /// </summary>
    Task<AuthTokens> GenerateTokensAsync(UserId userId, CancellationToken ct = default);

    /// <summary>
    /// Validates a refresh token and generates a new token pair.
    /// Returns the user ID alongside the tokens so the caller can load user data.
    /// </summary>
    Task<Result<(UserId UserId, AuthTokens Tokens), DomainError>> RefreshTokenAsync(
        string refreshToken, CancellationToken ct = default);

    /// <summary>Revokes a refresh token (idempotent — succeeds even if token is not found).</summary>
    Task<Result<DomainError>> RevokeRefreshTokenAsync(
        string refreshToken, CancellationToken ct = default);

    /// <summary>Verifies a user's password (for break-the-glass re-authentication).</summary>
    Task<Result<DomainError>> VerifyPasswordAsync(
        Guid userId, string password, CancellationToken ct = default);
}
