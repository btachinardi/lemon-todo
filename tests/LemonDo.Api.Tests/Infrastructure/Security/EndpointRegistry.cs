namespace LemonDo.Api.Tests.Infrastructure.Security;

/// <summary>
/// Central catalog of all API endpoints for parameterized security testing.
/// Each entry specifies the path, method, body, auth level, denied methods, and pagination.
/// </summary>
public static class EndpointRegistry
{
    // ─── Task Endpoints ──────────────────────────────────────────
    private static readonly EndpointDescriptor ListTasks = new()
    {
        Path = "/api/tasks",
        Method = HttpMethod.Get,
        RequiredAuth = AuthLevel.User,
        IsPaginated = true,
        DeniedMethods = [HttpMethod.Put, HttpMethod.Delete],
    };

    private static readonly EndpointDescriptor CreateTask = new()
    {
        Path = "/api/tasks",
        Method = HttpMethod.Post,
        Body = new { Title = "Security test" },
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor GetTask = new()
    {
        Path = "/api/tasks/{id}",
        Method = HttpMethod.Get,
        RequiredAuth = AuthLevel.User,
        DeniedMethods = [HttpMethod.Patch],
    };

    private static readonly EndpointDescriptor UpdateTask = new()
    {
        Path = "/api/tasks/{id}",
        Method = HttpMethod.Put,
        Body = new { Title = "Updated" },
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor DeleteTask = new()
    {
        Path = "/api/tasks/{id}",
        Method = HttpMethod.Delete,
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor CompleteTask = new()
    {
        Path = "/api/tasks/{id}/complete",
        Method = HttpMethod.Post,
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor UncompleteTask = new()
    {
        Path = "/api/tasks/{id}/uncomplete",
        Method = HttpMethod.Post,
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor ArchiveTask = new()
    {
        Path = "/api/tasks/{id}/archive",
        Method = HttpMethod.Post,
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor ViewNote = new()
    {
        Path = "/api/tasks/{id}/view-note",
        Method = HttpMethod.Post,
        Body = new { Password = "TestPass123!" },
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor BulkComplete = new()
    {
        Path = "/api/tasks/bulk/complete",
        Method = HttpMethod.Post,
        Body = new { TaskIds = new[] { Guid.NewGuid() } },
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor AddTag = new()
    {
        Path = "/api/tasks/{id}/tags",
        Method = HttpMethod.Post,
        Body = new { Tag = "test" },
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor RemoveTag = new()
    {
        Path = "/api/tasks/{id}/tags/{tag}",
        Method = HttpMethod.Delete,
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor MoveTask = new()
    {
        Path = "/api/tasks/{id}/move",
        Method = HttpMethod.Post,
        Body = new { ColumnId = Guid.NewGuid(), PreviousTaskId = (Guid?)null, NextTaskId = (Guid?)null },
        RequiredAuth = AuthLevel.User,
    };

    // ─── Board Endpoints ─────────────────────────────────────────
    private static readonly EndpointDescriptor GetDefaultBoard = new()
    {
        Path = "/api/boards/default",
        Method = HttpMethod.Get,
        RequiredAuth = AuthLevel.User,
        DeniedMethods = [HttpMethod.Put, HttpMethod.Delete],
    };

    private static readonly EndpointDescriptor GetBoardById = new()
    {
        Path = "/api/boards/{id}",
        Method = HttpMethod.Get,
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor AddColumn = new()
    {
        Path = "/api/boards/{id}/columns",
        Method = HttpMethod.Post,
        Body = new { Name = "Test", TargetStatus = "InProgress" },
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor RenameColumn = new()
    {
        Path = "/api/boards/{id}/columns/{colId}",
        Method = HttpMethod.Put,
        Body = new { Name = "Renamed" },
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor RemoveColumn = new()
    {
        Path = "/api/boards/{id}/columns/{colId}",
        Method = HttpMethod.Delete,
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor ReorderColumn = new()
    {
        Path = "/api/boards/{id}/columns/reorder",
        Method = HttpMethod.Post,
        Body = new { ColumnId = Guid.NewGuid(), NewPosition = 0 },
        RequiredAuth = AuthLevel.User,
    };

    // ─── Auth Endpoints (protected) ──────────────────────────────
    private static readonly EndpointDescriptor GetMe = new()
    {
        Path = "/api/auth/me",
        Method = HttpMethod.Get,
        RequiredAuth = AuthLevel.User,
        DeniedMethods = [HttpMethod.Post],
    };

    private static readonly EndpointDescriptor Logout = new()
    {
        Path = "/api/auth/logout",
        Method = HttpMethod.Post,
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor RevealProfile = new()
    {
        Path = "/api/auth/reveal-profile",
        Method = HttpMethod.Post,
        Body = new { Password = "TestPass123!" },
        RequiredAuth = AuthLevel.User,
    };

    // ─── Notification Endpoints ──────────────────────────────────
    private static readonly EndpointDescriptor ListNotifications = new()
    {
        Path = "/api/notifications",
        Method = HttpMethod.Get,
        RequiredAuth = AuthLevel.User,
        IsPaginated = true,
        DeniedMethods = [HttpMethod.Delete],
    };

    private static readonly EndpointDescriptor GetUnreadCount = new()
    {
        Path = "/api/notifications/unread-count",
        Method = HttpMethod.Get,
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor MarkNotificationRead = new()
    {
        Path = "/api/notifications/{id}/read",
        Method = HttpMethod.Post,
        RequiredAuth = AuthLevel.User,
    };

    private static readonly EndpointDescriptor MarkAllNotificationsRead = new()
    {
        Path = "/api/notifications/read-all",
        Method = HttpMethod.Post,
        RequiredAuth = AuthLevel.User,
    };

    // ─── Onboarding Endpoints ────────────────────────────────────
    private static readonly EndpointDescriptor GetOnboardingStatus = new()
    {
        Path = "/api/onboarding/status",
        Method = HttpMethod.Get,
        RequiredAuth = AuthLevel.User,
        DeniedMethods = [HttpMethod.Put],
    };

    private static readonly EndpointDescriptor CompleteOnboarding = new()
    {
        Path = "/api/onboarding/complete",
        Method = HttpMethod.Post,
        RequiredAuth = AuthLevel.User,
    };

    // ─── Analytics Endpoints ─────────────────────────────────────
    private static readonly EndpointDescriptor TrackEvents = new()
    {
        Path = "/api/analytics/events",
        Method = HttpMethod.Post,
        Body = new { Events = new[] { new { EventName = "test_event" } } },
        RequiredAuth = AuthLevel.User,
        DeniedMethods = [HttpMethod.Get],
    };

    // ─── Admin Endpoints ─────────────────────────────────────────
    private static readonly EndpointDescriptor AdminListUsers = new()
    {
        Path = "/api/admin/users",
        Method = HttpMethod.Get,
        RequiredAuth = AuthLevel.Admin,
        IsPaginated = true,
        DeniedMethods = [HttpMethod.Put],
    };

    private static readonly EndpointDescriptor AdminGetUser = new()
    {
        Path = "/api/admin/users/{id}",
        Method = HttpMethod.Get,
        RequiredAuth = AuthLevel.Admin,
    };

    private static readonly EndpointDescriptor AdminAssignRole = new()
    {
        Path = "/api/admin/users/{id}/roles",
        Method = HttpMethod.Post,
        Body = new { RoleName = "Admin" },
        RequiredAuth = AuthLevel.SystemAdmin,
    };

    private static readonly EndpointDescriptor AdminRemoveRole = new()
    {
        Path = "/api/admin/users/{id}/roles/{roleName}",
        Method = HttpMethod.Delete,
        RequiredAuth = AuthLevel.SystemAdmin,
    };

    private static readonly EndpointDescriptor AdminDeactivateUser = new()
    {
        Path = "/api/admin/users/{id}/deactivate",
        Method = HttpMethod.Post,
        RequiredAuth = AuthLevel.SystemAdmin,
        DeniedMethods = [HttpMethod.Delete],
    };

    private static readonly EndpointDescriptor AdminReactivateUser = new()
    {
        Path = "/api/admin/users/{id}/reactivate",
        Method = HttpMethod.Post,
        RequiredAuth = AuthLevel.SystemAdmin,
    };

    private static readonly EndpointDescriptor AdminRevealProtectedData = new()
    {
        Path = "/api/admin/users/{id}/reveal",
        Method = HttpMethod.Post,
        Body = new { Reason = "SupportTicket", Password = "SysAdminPass123!" },
        RequiredAuth = AuthLevel.SystemAdmin,
    };

    private static readonly EndpointDescriptor AdminRevealTaskNote = new()
    {
        Path = "/api/admin/tasks/{id}/reveal-note",
        Method = HttpMethod.Post,
        Body = new { Reason = "SupportTicket", Password = "SysAdminPass123!" },
        RequiredAuth = AuthLevel.SystemAdmin,
    };

    private static readonly EndpointDescriptor AdminAuditLog = new()
    {
        Path = "/api/admin/audit",
        Method = HttpMethod.Get,
        RequiredAuth = AuthLevel.Admin,
        IsPaginated = true,
        DeniedMethods = [HttpMethod.Post, HttpMethod.Delete, HttpMethod.Put],
    };

    // ─── Auth Endpoints (public, method enforcement only) ─────────
    private static readonly EndpointDescriptor AuthLogin = new()
    {
        Path = "/api/auth/login",
        Method = HttpMethod.Post,
        Body = new { Email = "test@lemondo.dev", Password = "TestPass123!" },
        RequiredAuth = AuthLevel.Anonymous,
        DeniedMethods = [HttpMethod.Get],
    };

    private static readonly EndpointDescriptor AuthRegister = new()
    {
        Path = "/api/auth/register",
        Method = HttpMethod.Post,
        Body = new { Email = "x@lemondo.dev", Password = "TestPass123!", DisplayName = "Test" },
        RequiredAuth = AuthLevel.Anonymous,
    };

    private static readonly EndpointDescriptor AuthRefresh = new()
    {
        Path = "/api/auth/refresh",
        Method = HttpMethod.Post,
        RequiredAuth = AuthLevel.Anonymous, // uses cookie, not bearer
    };

    // ─── Config Endpoint ─────────────────────────────────────────
    private static readonly EndpointDescriptor GetConfig = new()
    {
        Path = "/api/config",
        Method = HttpMethod.Get,
        RequiredAuth = AuthLevel.Anonymous,
        DeniedMethods = [HttpMethod.Post],
    };

    // ═════════════════════════════════════════════════════════════
    //  Computed collections for DynamicData
    // ═════════════════════════════════════════════════════════════

    /// <summary>All endpoints in the registry.</summary>
    public static IReadOnlyList<EndpointDescriptor> AllEndpoints { get; } =
    [
        // Tasks
        ListTasks, CreateTask, GetTask, UpdateTask, DeleteTask,
        CompleteTask, UncompleteTask, ArchiveTask, ViewNote,
        BulkComplete, AddTag, RemoveTag, MoveTask,
        // Boards
        GetDefaultBoard, GetBoardById, AddColumn, RenameColumn, RemoveColumn, ReorderColumn,
        // Auth (protected)
        GetMe, Logout, RevealProfile,
        // Notifications
        ListNotifications, GetUnreadCount, MarkNotificationRead, MarkAllNotificationsRead,
        // Onboarding
        GetOnboardingStatus, CompleteOnboarding,
        // Analytics
        TrackEvents,
        // Admin
        AdminListUsers, AdminGetUser, AdminAssignRole, AdminRemoveRole,
        AdminDeactivateUser, AdminReactivateUser, AdminRevealProtectedData,
        AdminRevealTaskNote, AdminAuditLog,
        // Auth (public)
        AuthLogin, AuthRegister, AuthRefresh,
        // Config
        GetConfig,
    ];

    /// <summary>All endpoints that require authentication (User, Admin, or SystemAdmin).</summary>
    public static IEnumerable<object[]> AuthenticatedEndpointData =>
        AllEndpoints
            .Where(e => e.RequiredAuth != AuthLevel.Anonymous)
            .Select(e => new object[] { e });

    /// <summary>All (path, deniedMethod) pairs for method enforcement testing.</summary>
    public static IEnumerable<object[]> MethodEnforcementData =>
        AllEndpoints
            .Where(e => e.DeniedMethods is not null)
            .SelectMany(e => e.DeniedMethods!.Select(
                denied => new object[] { e.Path, denied }));

    /// <summary>All paginated GET endpoints.</summary>
    public static IEnumerable<object[]> PaginatedEndpointData =>
        AllEndpoints
            .Where(e => e.IsPaginated)
            .Select(e => new object[] { e });

    /// <summary>Endpoints requiring Admin or SystemAdmin — regular User should get 403.</summary>
    public static IEnumerable<object[]> AdminOnlyEndpointData =>
        AllEndpoints
            .Where(e => e.RequiredAuth is AuthLevel.Admin or AuthLevel.SystemAdmin)
            .Select(e => new object[] { e });

    /// <summary>Endpoints requiring SystemAdmin — Admin should get 403.</summary>
    public static IEnumerable<object[]> SystemAdminOnlyEndpointData =>
        AllEndpoints
            .Where(e => e.RequiredAuth == AuthLevel.SystemAdmin)
            .Select(e => new object[] { e });

    /// <summary>Generates a readable display name for DynamicData test rows.</summary>
    public static string GetDisplayName(System.Reflection.MethodInfo _, object[] data)
    {
        if (data[0] is EndpointDescriptor ep)
            return ep.DisplayName;
        if (data[0] is string path && data[1] is HttpMethod method)
            return $"{method.Method} {path}";
        return string.Join(" | ", data.Select(d => d?.ToString() ?? "null"));
    }

    /// <summary>Generates display name for pagination cross-product tests.</summary>
    public static string GetPaginationDisplayName(System.Reflection.MethodInfo _, object[] data)
    {
        if (data[0] is EndpointDescriptor ep && data.Length >= 4 && data[3] is string desc)
            return $"{ep.DisplayName} [{desc}]";
        return string.Join(" | ", data.Select(d => d?.ToString() ?? "null"));
    }
}
