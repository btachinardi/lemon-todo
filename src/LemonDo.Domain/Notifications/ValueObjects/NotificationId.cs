namespace LemonDo.Domain.Notifications.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>Strongly-typed identifier for a <see cref="Entities.Notification"/>.</summary>
public sealed class NotificationId : ValueObject<Guid>, IReconstructable<NotificationId, Guid>
{
    /// <summary>Creates a new <see cref="NotificationId"/> with the given value.</summary>
    public NotificationId(Guid value) : base(value) { }

    /// <summary>Generates a fresh random identifier.</summary>
    public static NotificationId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing <see cref="Guid"/> as a <see cref="NotificationId"/>.</summary>
    public static NotificationId From(Guid value) => new(value);

    /// <inheritdoc />
    public static NotificationId Reconstruct(Guid value) => new(value);
}
