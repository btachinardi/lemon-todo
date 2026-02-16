namespace LemonDo.Domain.Identity.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;

/// <summary>Raised when a new user registers. Carries only the user ID — no PII.</summary>
public sealed record UserRegisteredEvent(UserId UserId) : DomainEvent;
