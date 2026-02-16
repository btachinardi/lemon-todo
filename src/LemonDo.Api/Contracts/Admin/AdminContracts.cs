namespace LemonDo.Api.Contracts.Admin;

/// <summary>Request body for assigning a role to a user.</summary>
public sealed record AssignRoleRequest(string RoleName);

/// <summary>Request body for revealing a user's PII with break-the-glass controls.</summary>
public sealed record RevealPiiRequest(string Reason, string? ReasonDetails, string? Comments, string Password);
