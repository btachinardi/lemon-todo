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
}
