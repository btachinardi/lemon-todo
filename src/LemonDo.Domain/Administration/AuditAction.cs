namespace LemonDo.Domain.Administration;

using System.Text.Json.Serialization;

/// <summary>
/// Categorizes security-relevant actions recorded in the audit trail.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuditAction
{
    /// <summary>A new user account was created.</summary>
    UserRegistered,

    /// <summary>A user authenticated successfully.</summary>
    UserLoggedIn,

    /// <summary>A user signed out.</summary>
    UserLoggedOut,

    /// <summary>A role was assigned to a user.</summary>
    RoleAssigned,

    /// <summary>A role was removed from a user.</summary>
    RoleRemoved,

    /// <summary>An admin revealed redacted protected data.</summary>
    ProtectedDataRevealed,

    /// <summary>A new task was created.</summary>
    TaskCreated,

    /// <summary>A task was marked as completed.</summary>
    TaskCompleted,

    /// <summary>A task was permanently deleted.</summary>
    TaskDeleted,

    /// <summary>A user account was deactivated.</summary>
    UserDeactivated,

    /// <summary>A user account was reactivated.</summary>
    UserReactivated,

    /// <summary>The system decrypted protected data for an automated operation (e.g., sending email).</summary>
    ProtectedDataAccessed,

    /// <summary>A task's sensitive note was viewed by its owner or revealed by an admin.</summary>
    SensitiveNoteRevealed,
}
