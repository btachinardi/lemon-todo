namespace LemonDo.Api.Contracts;

/// <summary>Views a task's sensitive note via break-the-glass authentication.</summary>
/// <remarks>
/// Password is the current user's password for re-authentication.
/// Creates an audit trail entry for compliance.
/// </remarks>
public sealed record ViewTaskNoteRequest(string Password);
