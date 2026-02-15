namespace LemonDo.Domain.Identity.Entities;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Events;
using LemonDo.Domain.Identity.ValueObjects;

/// <summary>
/// Domain user entity. Pure domain model with no ASP.NET Identity dependency.
/// The Infrastructure layer maps this to/from <c>ApplicationUser</c> (IdentityUser).
/// </summary>
public sealed class User : Entity<UserId>
{
    /// <summary>The user's email address.</summary>
    public Email Email { get; private set; }

    /// <summary>The user's display name.</summary>
    public DisplayName DisplayName { get; private set; }

    private User(UserId id, Email email, DisplayName displayName)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
    }

    /// <summary>Creates a new user and raises a <see cref="UserRegisteredEvent"/>.</summary>
    public static Result<User, DomainError> Create(Email email, DisplayName displayName)
    {
        var user = new User(UserId.New(), email, displayName);
        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, email.Value, displayName.Value));
        return Result<User, DomainError>.Success(user);
    }

    /// <summary>Creates a user from existing data (e.g., loaded from database). No events raised.</summary>
    public static User Reconstitute(UserId id, Email email, DisplayName displayName) =>
        new(id, email, displayName);
}
