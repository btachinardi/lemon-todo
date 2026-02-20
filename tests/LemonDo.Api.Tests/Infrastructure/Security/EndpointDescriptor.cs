namespace LemonDo.Api.Tests.Infrastructure.Security;

/// <summary>
/// Describes a single API endpoint for parameterized security testing.
/// </summary>
public sealed record EndpointDescriptor
{
    /// <summary>The URL path (e.g. <c>/api/tasks</c>). May contain <c>{id}</c> placeholders
    /// that are auto-replaced with a random GUID.</summary>
    public required string Path { get; init; }

    /// <summary>The HTTP method this endpoint accepts.</summary>
    public required HttpMethod Method { get; init; }

    /// <summary>Optional JSON-serializable body to include with POST/PUT requests.</summary>
    public object? Body { get; init; }

    /// <summary>Minimum auth level required to access this endpoint.</summary>
    public required AuthLevel RequiredAuth { get; init; }

    /// <summary>HTTP methods that should return 405 on this path.</summary>
    public HttpMethod[]? DeniedMethods { get; init; }

    /// <summary>Whether this endpoint returns paginated results.</summary>
    public bool IsPaginated { get; init; }

    /// <summary>Human-readable label for test display.</summary>
    public string DisplayName => $"{Method.Method} {Path}";

    /// <summary>Returns the path with any <c>{id}</c> placeholders replaced with random GUIDs.</summary>
    public string ResolvedPath => Path
        .Replace("{id}", Guid.NewGuid().ToString())
        .Replace("{colId}", Guid.NewGuid().ToString())
        .Replace("{tag}", "sometag")
        .Replace("{roleName}", "Admin");
}

/// <summary>Minimum authorization level required for an endpoint.</summary>
public enum AuthLevel
{
    /// <summary>No authentication required.</summary>
    Anonymous,
    /// <summary>Any authenticated user.</summary>
    User,
    /// <summary>Admin or SystemAdmin role required.</summary>
    Admin,
    /// <summary>SystemAdmin role required.</summary>
    SystemAdmin
}
