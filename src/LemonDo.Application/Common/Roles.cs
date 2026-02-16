namespace LemonDo.Application.Common;

/// <summary>
/// Application role constants. Used for role seeding and authorization policies.
/// </summary>
public static class Roles
{
    /// <summary>Default role assigned to all registered users.</summary>
    public const string User = "User";

    /// <summary>Administrative role with access to the admin panel and user management (read-only protected data).</summary>
    public const string Admin = "Admin";

    /// <summary>Elevated role with full system access including protected data reveal, role assignment, and user deactivation.</summary>
    public const string SystemAdmin = "SystemAdmin";

    /// <summary>Policy name requiring Admin or SystemAdmin role.</summary>
    public const string RequireAdminOrAbove = "RequireAdminOrAbove";

    /// <summary>Policy name requiring SystemAdmin role only.</summary>
    public const string RequireSystemAdmin = "RequireSystemAdmin";
}
