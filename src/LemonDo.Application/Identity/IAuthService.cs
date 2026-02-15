namespace LemonDo.Application.Identity;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;

/// <summary>
/// Anti-Corruption Layer port for authentication operations.
/// The Application layer calls this interface; the Infrastructure layer implements it
/// using ASP.NET Identity, SignInManager, and JWT token services.
/// </summary>
public interface IAuthService
{
    /// <summary>Registers a new user account with Identity and generates authentication tokens.</summary>
    Task<Result<AuthResult, DomainError>> RegisterAsync(
        UserId userId, Email email, string password, DisplayName displayName,
        CancellationToken ct = default);

    /// <summary>Authenticates a user by email and password. Supports account lockout.</summary>
    Task<Result<AuthResult, DomainError>> LoginAsync(
        string email, string password,
        CancellationToken ct = default);

    /// <summary>Exchanges a valid refresh token for a new token pair.</summary>
    Task<Result<AuthResult, DomainError>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken ct = default);

    /// <summary>Revokes a refresh token (idempotent — succeeds even if token is not found).</summary>
    Task<Result<DomainError>> RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken ct = default);
}
