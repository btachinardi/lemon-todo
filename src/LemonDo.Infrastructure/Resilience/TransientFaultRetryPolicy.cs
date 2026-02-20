namespace LemonDo.Infrastructure.Resilience;

using Microsoft.Extensions.Logging;

/// <summary>
/// A simple retry policy for transient database faults. Wraps an async operation
/// and retries up to <see cref="MaxRetries"/> times with exponential back-off
/// when <see cref="SqliteTransientFaultDetector.IsTransient"/> returns <c>true</c>.
/// </summary>
/// <remarks>
/// This policy is designed for service-level operations that go through both EF Core
/// and ASP.NET Identity (which internally uses EF Core). It operates ABOVE the DbContext
/// and does not conflict with EF Core's execution strategy or explicit transactions.
/// </remarks>
public sealed class TransientFaultRetryPolicy(ILogger<TransientFaultRetryPolicy> logger)
{
    private static readonly Random Jitter = new();

    /// <summary>Maximum number of retry attempts before giving up.</summary>
    public int MaxRetries { get; init; } = 5;

    /// <summary>Base delay between retries (multiplied by attempt number for linear back-off, plus jitter).</summary>
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Executes the given operation with transient fault retry.
    /// Returns the operation result on success, or re-throws the last exception
    /// if all retries are exhausted. Uses linear back-off with random jitter
    /// to avoid thundering herd when many concurrent operations retry.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct = default)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < MaxRetries && SqliteTransientFaultDetector.IsTransient(ex))
            {
                var baseMs = BaseDelay.TotalMilliseconds * (attempt + 1);
                int jitterMs;
                lock (Jitter) { jitterMs = Jitter.Next(0, (int)baseMs); }
                var delayMs = baseMs + jitterMs;
                logger.LogDebug(
                    "Transient SQLite fault (attempt {Attempt}/{MaxRetries}), retrying in {DelayMs:F0}ms: {Message}",
                    attempt + 1, MaxRetries, delayMs, ex.Message);
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), ct);
            }
        }
    }

    /// <summary>
    /// Executes the given void operation with transient fault retry.
    /// </summary>
    public async Task ExecuteAsync(Func<Task> operation, CancellationToken ct = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true; // dummy return value
        }, ct);
    }
}
