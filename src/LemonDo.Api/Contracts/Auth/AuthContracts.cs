namespace LemonDo.Api.Contracts.Auth;

/// <summary>Request to register a new user account.</summary>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);

/// <summary>Request to authenticate with email and password.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Request to exchange a refresh token for a new access token.</summary>
public sealed record RefreshRequest(string RefreshToken);

/// <summary>Response containing authentication tokens.</summary>
public sealed record AuthResponse(string AccessToken, string RefreshToken, UserResponse User);

/// <summary>Response containing user profile information.</summary>
public sealed record UserResponse(Guid Id, string Email, string DisplayName);
