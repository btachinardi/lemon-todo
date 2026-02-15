namespace LemonDo.Application.Identity;

/// <summary>Successful authentication result with opaque token strings.</summary>
public sealed record AuthResult(
    Guid UserId,
    string Email,
    string DisplayName,
    string AccessToken,
    string RefreshToken);
