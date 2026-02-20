namespace LemonDo.Api.Endpoints;

using System.Security.Claims;
using LemonDo.Application.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Notifications.Repositories;
using LemonDo.Domain.Notifications.ValueObjects;

/// <summary>Minimal API endpoints for in-app notifications.</summary>
public static class NotificationEndpoints
{
    /// <summary>Maps notification endpoints under <c>/api/notifications</c>.</summary>
    public static RouteGroupBuilder MapNotificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("/", ListNotifications).Produces<NotificationListResponse>();
        group.MapGet("/unread-count", GetUnreadCount).Produces<UnreadCountResponse>();
        group.MapPost("/{id}/read", MarkAsRead);
        group.MapPost("/read-all", MarkAllAsRead);

        return group;
    }

    private static async Task<IResult> ListNotifications(
        ClaimsPrincipal principal,
        INotificationRepository notificationRepository,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        if (pageSize > 200)
            return Results.BadRequest(new { Error = "pageSize must not exceed 200." });

        pageSize = Math.Clamp(pageSize, 1, 200);
        page = Math.Clamp(page, 1, int.MaxValue / pageSize);

        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var result = await notificationRepository.ListAsync(userId, page, pageSize, ct);

        var items = result.Items.Select(n => new NotificationResponse(
            n.Id.Value.ToString(),
            n.Type.ToString(),
            n.Title,
            n.Body,
            n.IsRead,
            n.ReadAt?.ToString("O"),
            n.CreatedAt.ToString("O"))).ToList();

        return Results.Ok(new NotificationListResponse(items, result.TotalCount, result.Page, result.PageSize));
    }

    private static async Task<IResult> GetUnreadCount(
        ClaimsPrincipal principal,
        INotificationRepository notificationRepository,
        CancellationToken ct = default)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var count = await notificationRepository.GetUnreadCountAsync(userId, ct);
        return Results.Ok(new UnreadCountResponse(count));
    }

    private static async Task<IResult> MarkAsRead(
        string id,
        ClaimsPrincipal principal,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        CancellationToken ct = default)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        if (!Guid.TryParse(id, out var guid))
            return Results.BadRequest(new { Error = "Invalid notification id. A valid GUID is required." });

        var notification = await notificationRepository.GetByIdAsync(NotificationId.From(guid), ct);
        if (notification is null || notification.UserId != userId)
            return Results.NotFound();

        notification.MarkAsRead();
        await unitOfWork.SaveChangesAsync(ct);

        return Results.Ok();
    }

    private static async Task<IResult> MarkAllAsRead(
        ClaimsPrincipal principal,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        CancellationToken ct = default)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        await notificationRepository.MarkAllAsReadAsync(userId, ct);
        return Results.Ok();
    }

    private static UserId? GetUserId(ClaimsPrincipal principal)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr is null || !Guid.TryParse(userIdStr, out var guid))
            return null;
        return UserId.Reconstruct(guid);
    }
}

/// <summary>Single notification in a list response.</summary>
/// <param name="Id">Notification unique identifier.</param>
/// <param name="Type">Notification type (DueDateReminder, TaskOverdue, Welcome).</param>
/// <param name="Title">Short headline text.</param>
/// <param name="Body">Optional longer description.</param>
/// <param name="IsRead">Whether the user has read this notification.</param>
/// <param name="ReadAt">ISO 8601 timestamp of when the notification was read, or null.</param>
/// <param name="CreatedAt">ISO 8601 timestamp of creation.</param>
public sealed record NotificationResponse(
    string Id,
    string Type,
    string Title,
    string? Body,
    bool IsRead,
    string? ReadAt,
    string CreatedAt);

/// <summary>Paginated list of notifications.</summary>
/// <param name="Items">The notification items for this page.</param>
/// <param name="TotalCount">Total number of notifications.</param>
/// <param name="Page">Current page number.</param>
/// <param name="PageSize">Items per page.</param>
public sealed record NotificationListResponse(
    List<NotificationResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>Count of unread notifications.</summary>
/// <param name="Count">Number of unread notifications.</param>
public sealed record UnreadCountResponse(int Count);
