namespace LemonDo.Application.Common;

/// <summary>
/// Provides request-scoped context for audit trail entries.
/// Extracted from HttpContext in the API layer.
/// </summary>
public interface IRequestContext
{
    /// <summary>The authenticated user's ID, or null for anonymous requests.</summary>
    Guid? UserId { get; }

    /// <summary>The client IP address.</summary>
    string? IpAddress { get; }

    /// <summary>The client User-Agent header.</summary>
    string? UserAgent { get; }
}
