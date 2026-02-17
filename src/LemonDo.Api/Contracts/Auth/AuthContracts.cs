namespace LemonDo.Api.Contracts.Auth;

using LemonDo.Domain.Common;

/// <summary>Registers a new user account with email and password.</summary>
/// <remarks>
/// Password must be at least 8 characters with uppercase, lowercase, digit, and special character.
/// Email must be a valid email address format.
/// DisplayName must be 1-100 characters.
/// Validation and encryption happen at JSON deserialization via protected data converters.
/// </remarks>
public sealed record RegisterRequest(EncryptedField Email, ProtectedValue Password, EncryptedField DisplayName);

/// <summary>Authenticates a user with email and password credentials.</summary>
public sealed record LoginRequest(EncryptedField Email, ProtectedValue Password);

/// <summary>Authentication response containing access token and user profile.</summary>
/// <remarks>
/// AccessToken is returned in the response body and should be sent as Bearer token in Authorization header.
/// Refresh token is set as HttpOnly cookie with SameSite=Strict and Path=/api/auth.
/// </remarks>
public sealed record AuthResponse(string AccessToken, UserResponse User);

/// <summary>User profile information with redacted personal data.</summary>
/// <remarks>
/// Email and DisplayName are redacted forms of the original values for privacy.
/// </remarks>
public sealed record UserResponse(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);

/// <summary>Request body for <c>POST /api/auth/reveal-profile</c>.</summary>
public sealed record RevealProfileRequest(ProtectedValue Password);

/// <summary>Response containing the user's unredacted profile data after password re-authentication.</summary>
public sealed record RevealedProfileResponse(RevealedField Email, RevealedField DisplayName);
