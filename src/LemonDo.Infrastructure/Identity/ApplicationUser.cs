namespace LemonDo.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;

/// <summary>
/// ASP.NET Core Identity user — credentials and authorization only.
/// <list type="bullet">
///   <item><c>UserName</c> → SHA-256 hash of email (for <c>FindByNameAsync</c> lookups).</item>
///   <item>Password hash, lockout, roles — managed by Identity.</item>
/// </list>
/// All user profile data (email, display name, deactivation) lives on the domain
/// <see cref="LemonDo.Domain.Identity.Entities.User"/> entity in the <c>Users</c> table.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
}
