namespace LemonDo.Api.Contracts.Admin;

/// <summary>Assigns a role to a user account.</summary>
/// <remarks>
/// Valid role names: "User", "Admin".
/// </remarks>
public sealed record AssignRoleRequest(string RoleName);

/// <summary>Reveals a user's protected data via break-the-glass authentication.</summary>
/// <remarks>
/// Reason must be a valid AccessReason enum value (e.g., "LegalCompliance", "SecurityIncident", "UserRequest").
/// Password is the admin's current password for re-authentication.
/// Creates an audit trail entry.
/// </remarks>
public sealed record RevealProtectedDataRequest(string Reason, string? ReasonDetails, string? Comments, string Password);

/// <summary>Reveals a task's sensitive note via break-the-glass authentication.</summary>
/// <remarks>
/// Reason must be a valid AccessReason enum value (e.g., "LegalCompliance", "SecurityIncident", "UserRequest").
/// Password is the admin's current password for re-authentication.
/// Creates an audit trail entry.
/// </remarks>
public sealed record RevealTaskNoteRequest(string Reason, string? ReasonDetails, string? Comments, string Password);
