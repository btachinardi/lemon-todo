namespace LemonDo.Api.Contracts.Auth;

/// <summary>Request to register a new user account.</summary>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);

/// <summary>Request to authenticate with email and password.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Response containing an access token (refresh token is set as HttpOnly cookie).</summary>
public sealed record AuthResponse(string AccessToken, UserResponse User);

/// <summary>Response containing user profile information.</summary>
public sealed record UserResponse(Guid Id, string Email, string DisplayName);
