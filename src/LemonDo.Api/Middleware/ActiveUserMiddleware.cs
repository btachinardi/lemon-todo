namespace LemonDo.Api.Middleware;

using System.Security.Claims;
using LemonDo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

/// <summary>
/// Rejects authenticated requests from permanently deactivated users.
/// Runs after UseAuthentication/UseAuthorization so that claims are populated.
/// Returns 401 if the user's account has been deactivated (LockoutEnd = MaxValue).
/// Does NOT block temporarily locked users (rate-limit lockout from failed attempts) —
/// those users can still use their existing access tokens; lockout only affects
/// password-based operations.
/// </summary>
public sealed class ActiveUserMiddleware(RequestDelegate next)
{
    /// <summary>Checks if the authenticated user is deactivated; returns 401 if so.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim is not null && Guid.TryParse(userIdClaim, out var claimGuid) && claimGuid != Guid.Empty)
            {
                var userManager = context.RequestServices
                    .GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByIdAsync(userIdClaim);
                if (user is not null && IsDeactivated(user))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }
            else if (userIdClaim is not null)
            {
                // Sub claim present but not a valid non-empty GUID — reject as unauthorized.
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await next(context);
    }

    /// <summary>
    /// Distinguishes permanent deactivation from temporary lockout.
    /// Deactivation sets LockoutEnd to <see cref="DateTimeOffset.MaxValue"/>.
    /// Temporary lockout uses short windows (e.g. 15 minutes).
    /// A lockout more than 1 year in the future is treated as permanent deactivation.
    /// </summary>
    private static bool IsDeactivated(ApplicationUser user) =>
        user.LockoutEnd.HasValue
        && user.LockoutEnd.Value > DateTimeOffset.UtcNow.AddYears(1);
}
