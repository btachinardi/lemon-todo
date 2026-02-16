namespace LemonDo.Api.Contracts.Admin;

/// <summary>Request body for assigning a role to a user.</summary>
public sealed record AssignRoleRequest(string RoleName);

/// <summary>Request body for revealing a user's protected data with break-the-glass controls.</summary>
public sealed record RevealProtectedDataRequest(string Reason, string? ReasonDetails, string? Comments, string Password);
