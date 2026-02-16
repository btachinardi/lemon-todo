namespace LemonDo.Infrastructure.Notifications;

using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Notifications.Entities;
using LemonDo.Domain.Notifications.Enums;
using LemonDo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using TaskStatus = LemonDo.Domain.Tasks.ValueObjects.TaskStatus;

/// <summary>
/// Background service that periodically checks for tasks due within 24 hours
/// and creates in-app notifications for their owners.
/// Runs every 6 hours. Skips tasks that already have an active reminder notification.
/// </summary>
public sealed class DueDateReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<DueDateReminderService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DueDateReminderService started. Checking every {Interval}", Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckDueDatesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error checking due dates for reminders");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CheckDueDatesAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LemonDoDbContext>();

        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddHours(24);

        // Find tasks due within 24 hours that are not done/deleted
        var tasksDueSoon = await context.Tasks
            .Where(t => t.DueDate != null
                      && t.DueDate <= cutoff
                      && t.DueDate > now
                      && t.Status != TaskStatus.Done
                      && !t.IsDeleted)
            .Select(t => new { t.Id, t.OwnerId, t.Title, t.DueDate })
            .ToListAsync(ct);

        // Find tasks that are overdue
        var tasksOverdue = await context.Tasks
            .Where(t => t.DueDate != null
                      && t.DueDate <= now
                      && t.Status != TaskStatus.Done
                      && !t.IsDeleted)
            .Select(t => new { t.Id, t.OwnerId, t.Title, t.DueDate })
            .ToListAsync(ct);

        // Get existing notification titles to avoid duplicates (simple dedup by title)
        var existingTitles = await context.Notifications
            .Where(n => n.CreatedAt > now.AddHours(-24))
            .Select(n => new { n.UserId, n.Title })
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(
            existingTitles.Select(e => $"{e.UserId.Value}:{e.Title}"));

        var created = 0;

        foreach (var task in tasksDueSoon)
        {
            var title = $"Task due soon: {task.Title}";
            var key = $"{task.OwnerId.Value}:{title}";
            if (existingSet.Contains(key)) continue;

            var notification = Notification.Create(
                task.OwnerId,
                NotificationType.DueDateReminder,
                title,
                $"'{task.Title}' is due within the next 24 hours.");

            await context.Notifications.AddAsync(notification, ct);
            existingSet.Add(key);
            created++;
        }

        foreach (var task in tasksOverdue)
        {
            var title = $"Task overdue: {task.Title}";
            var key = $"{task.OwnerId.Value}:{title}";
            if (existingSet.Contains(key)) continue;

            var notification = Notification.Create(
                task.OwnerId,
                NotificationType.TaskOverdue,
                title,
                $"'{task.Title}' has passed its due date.");

            await context.Notifications.AddAsync(notification, ct);
            existingSet.Add(key);
            created++;
        }

        if (created > 0)
        {
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Created {Count} due-date reminder notifications", created);
        }
    }
}
