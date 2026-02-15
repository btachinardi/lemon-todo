namespace LemonDo.Domain.Identity.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;

/// <summary>Raised when a new user registers.</summary>
public sealed record UserRegisteredEvent(
    UserId UserId,
    string Email,
    string DisplayName) : DomainEvent;
