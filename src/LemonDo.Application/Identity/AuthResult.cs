namespace LemonDo.Application.Identity;

/// <summary>
/// Successful authentication result with opaque token strings.
/// Email and display name are always in redacted form — no plaintext protected data.
/// </summary>
public sealed record AuthResult(
    Guid UserId,
    string RedactedEmail,
    string RedactedDisplayName,
    IReadOnlyList<string> Roles,
    string AccessToken,
    string RefreshToken);
