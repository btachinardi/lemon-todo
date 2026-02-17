namespace LemonDo.Domain.Identity.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;

/// <summary>Raised when a user completes the onboarding tour.</summary>
public sealed record OnboardingCompletedEvent(UserId UserId) : DomainEvent;
