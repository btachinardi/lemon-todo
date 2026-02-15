namespace LemonDo.Application.Common;

using LemonDo.Domain.Identity.ValueObjects;

/// <summary>Provides the currently authenticated user's identity.</summary>
public interface ICurrentUserService
{
    /// <summary>The authenticated user's ID.</summary>
    UserId UserId { get; }

    /// <summary>Whether a user is currently authenticated.</summary>
    bool IsAuthenticated { get; }
}
