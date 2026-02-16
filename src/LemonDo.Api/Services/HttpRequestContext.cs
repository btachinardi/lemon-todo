namespace LemonDo.Api.Services;

using System.Security.Claims;
using LemonDo.Application.Common;

/// <summary>
/// Implements <see cref="IRequestContext"/> by extracting values from the current
/// <see cref="HttpContext"/>. Provides actor ID, IP address, and user agent for audit trail.
/// </summary>
public sealed class HttpRequestContext(IHttpContextAccessor httpContextAccessor) : IRequestContext
{
    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    /// <inheritdoc />
    public string? UserAgent =>
        httpContextAccessor.HttpContext?.Request.Headers.UserAgent.FirstOrDefault();
}
