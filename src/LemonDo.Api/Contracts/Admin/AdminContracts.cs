namespace LemonDo.Api.Contracts.Admin;

/// <summary>Request body for assigning a role to a user.</summary>
public sealed record AssignRoleRequest(string RoleName);
