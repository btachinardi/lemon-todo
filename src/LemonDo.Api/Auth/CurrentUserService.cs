namespace LemonDo.Api.Auth;

using System.Security.Claims;
using LemonDo.Application.Common;
using LemonDo.Domain.Identity.ValueObjects;

/// <summary>Reads the current user from the JWT claims in the HTTP context.</summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    /// <inheritdoc />
    public UserId UserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim is not null && Guid.TryParse(claim, out var guid)
                ? new UserId(guid)
                : throw new InvalidOperationException(
                    "No authenticated user. Ensure this service is only used behind RequireAuthorization().");
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
