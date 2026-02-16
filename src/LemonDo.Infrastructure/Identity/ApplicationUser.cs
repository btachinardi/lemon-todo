namespace LemonDo.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;

/// <summary>
/// ASP.NET Core Identity user with additional domain properties.
/// Maps to/from the domain <see cref="LemonDo.Domain.Identity.Entities.User"/> entity.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>The user's display name (2–100 chars).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>When the user account was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Whether the user has been deactivated by an admin (permanent lockout).</summary>
    public bool IsDeactivated { get; set; }
}
