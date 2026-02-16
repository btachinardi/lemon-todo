namespace LemonDo.Application.Administration.DTOs;

/// <summary>Read model for user data in admin views. Protected data fields are redacted by default.</summary>
public sealed record AdminUserDto
{
    /// <summary>User's unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>User's email (redacted by default, e.g. "j***@example.com").</summary>
    public required string Email { get; init; }

    /// <summary>User's display name (redacted by default, e.g. "J***e").</summary>
    public required string DisplayName { get; init; }

    /// <summary>Roles assigned to the user.</summary>
    public required IReadOnlyList<string> Roles { get; init; }

    /// <summary>Whether the user account is active (not locked out permanently).</summary>
    public required bool IsActive { get; init; }

    /// <summary>When the user account was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
