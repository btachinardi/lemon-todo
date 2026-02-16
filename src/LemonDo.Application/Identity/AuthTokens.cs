namespace LemonDo.Application.Identity;

/// <summary>
/// Pure authentication output — JWT access token, refresh token, and roles.
/// Contains no user data (no email, no display name). User data comes from the domain User entity.
/// </summary>
public sealed record AuthTokens(
    string AccessToken,
    string RefreshToken,
    IReadOnlyList<string> Roles);
