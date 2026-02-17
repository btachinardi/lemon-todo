using System.Security.Cryptography;
using System.Text;
using LemonDo.Application.Common;
using Microsoft.Extensions.Logging;

namespace LemonDo.Infrastructure.Analytics;

/// <summary>
/// Analytics adapter that logs events to structured logging.
/// User IDs are SHA-256 hashed to preserve privacy.
/// </summary>
public sealed class ConsoleAnalyticsService(ILogger<ConsoleAnalyticsService> logger) : IAnalyticsService
{
    /// <inheritdoc />
    public Task TrackAsync(
        string eventName,
        Guid? userId = null,
        Dictionary<string, string>? properties = null,
        CancellationToken ct = default)
    {
        var hashedUserId = userId.HasValue ? HashUserId(userId.Value) : "anonymous";

        logger.LogInformation(
            "Analytics: {EventName} | User: {HashedUserId} | Properties: {@Properties}",
            eventName,
            hashedUserId,
            properties ?? new Dictionary<string, string>());

        return Task.CompletedTask;
    }

    internal static string HashUserId(Guid userId)
    {
        var bytes = Encoding.UTF8.GetBytes(userId.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash)[..16]; // first 16 hex chars
    }
}
