namespace LemonDo.Domain.Administration.Entities;

using LemonDo.Domain.Administration.ValueObjects;
using LemonDo.Domain.Common;

/// <summary>
/// Records a security-relevant action in the system. Immutable once created.
/// </summary>
public sealed class AuditEntry : Entity<AuditEntryId>
{
    /// <summary>The user who performed the action (null for system-initiated).</summary>
    public Guid? ActorId { get; }

    /// <summary>The type of action that was performed.</summary>
    public AuditAction Action { get; }

    /// <summary>The type of resource affected (e.g., "Task", "User").</summary>
    public string ResourceType { get; }

    /// <summary>The ID of the affected resource.</summary>
    public string? ResourceId { get; }

    /// <summary>Optional JSON details about the action.</summary>
    public string? Details { get; }

    /// <summary>The IP address of the client that triggered the action.</summary>
    public string? IpAddress { get; }

    /// <summary>The User-Agent of the client that triggered the action.</summary>
    public string? UserAgent { get; }

    private AuditEntry(
        AuditEntryId id,
        Guid? actorId,
        AuditAction action,
        string resourceType,
        string? resourceId,
        string? details,
        string? ipAddress,
        string? userAgent)
        : base(id)
    {
        ActorId = actorId;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Details = details;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    /// <summary>Creates a new audit entry.</summary>
    public static AuditEntry Create(
        Guid? actorId,
        AuditAction action,
        string resourceType,
        string? resourceId = null,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new AuditEntry(
            AuditEntryId.New(),
            actorId,
            action,
            resourceType,
            resourceId,
            details,
            ipAddress,
            userAgent);
    }

    // EF Core constructor
#pragma warning disable CS8618
    private AuditEntry() : base(AuditEntryId.New()) { }
#pragma warning restore CS8618
}
