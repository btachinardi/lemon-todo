namespace LemonDo.Domain.Identity.Entities;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Events;
using LemonDo.Domain.Identity.ValueObjects;

/// <summary>
/// Domain user aggregate root. Owns all user profile data (email, display name, account status).
/// Persisted to its own <c>Users</c> table. ASP.NET Identity only handles credentials and roles.
/// </summary>
public sealed class User : Entity<UserId>
{
    /// <summary>Redacted email for safe display/logging (e.g. "j***@example.com").</summary>
    public string RedactedEmail { get; private set; }

    /// <summary>Redacted display name for safe display/logging (e.g. "J***e").</summary>
    public string RedactedDisplayName { get; private set; }

    /// <summary>Whether the user has been deactivated by an admin.</summary>
    public bool IsDeactivated { get; private set; }

    /// <summary>Timestamp when the user completed the onboarding tour. Null if not yet completed.</summary>
    public DateTimeOffset? OnboardingCompletedAt { get; private set; }

    private User(UserId id, string redactedEmail, string redactedDisplayName)
        : base(id)
    {
        RedactedEmail = redactedEmail;
        RedactedDisplayName = redactedDisplayName;
    }

    /// <summary>
    /// Creates a new user. Validates VOs, stores only redacted forms.
    /// Full protected data is passed to the repository for encryption during persistence.
    /// </summary>
    public static Result<User, DomainError> Create(Email email, DisplayName displayName)
    {
        var user = new User(UserId.New(), email.Redacted, displayName.Redacted);
        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id));
        return Result<User, DomainError>.Success(user);
    }

    /// <summary>Reconstructs from persistence. No events raised.</summary>
    public static User Reconstitute(
        UserId id, string redactedEmail, string redactedDisplayName,
        bool isDeactivated, DateTimeOffset? onboardingCompletedAt) =>
        new(id, redactedEmail, redactedDisplayName)
        {
            IsDeactivated = isDeactivated,
            OnboardingCompletedAt = onboardingCompletedAt,
        };

    /// <summary>Deactivates the user account (admin action).</summary>
    public Result<DomainError> Deactivate()
    {
        if (IsDeactivated)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("user.deactivation", "User is already deactivated."));

        IsDeactivated = true;
        return Result<DomainError>.Success();
    }

    /// <summary>Marks the onboarding tour as completed. Idempotent — no-ops if already completed.</summary>
    public void CompleteOnboarding()
    {
        if (OnboardingCompletedAt.HasValue) return;
        OnboardingCompletedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new OnboardingCompletedEvent(Id));
    }

    /// <summary>Reactivates a deactivated user account (admin action).</summary>
    public Result<DomainError> Reactivate()
    {
        if (!IsDeactivated)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("user.reactivation", "User is not deactivated."));

        IsDeactivated = false;
        return Result<DomainError>.Success();
    }
}
