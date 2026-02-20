namespace LemonDo.Infrastructure.Resilience;

/// <summary>
/// Detects transient SQLite errors anywhere in an exception chain.
/// Used by <see cref="TransientFaultRetryPolicy"/> to decide whether an
/// operation should be retried.
/// </summary>
/// <remarks>
/// SQLite in-memory databases share a single physical connection. Under concurrent
/// access, operations can fail with transient errors that succeed on retry:
///   - SqliteException with codes 1 (SQLITE_ERROR), 5 (SQLITE_BUSY), 6 (SQLITE_LOCKED)
///   - Various exceptions (InvalidOperationException, ArgumentOutOfRangeException,
///     NullReferenceException, etc.) from corrupted internal state when multiple threads
///     share the same physical connection
/// These errors are inherent to SQLite's single-writer architecture and are NOT bugs.
/// The detector uses specific error codes for SqliteException, and stack-trace origin
/// matching for all other exception types — if the exception originated from the SQLite
/// driver or EF Core storage/query layers, it's classified as transient.
/// </remarks>
public static class SqliteTransientFaultDetector
{
    private static readonly HashSet<int> TransientSqliteErrorCodes = [1, 5, 6];

    /// <summary>
    /// Returns <c>true</c> if the exception (or any inner exception) represents
    /// a transient SQLite error that would likely succeed on retry.
    /// </summary>
    public static bool IsTransient(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is Microsoft.Data.Sqlite.SqliteException sqliteEx
                && TransientSqliteErrorCodes.Contains(sqliteEx.SqliteErrorCode))
                return true;

            // EF Core wraps SQLite errors in DbUpdateException
            if (current is Microsoft.EntityFrameworkCore.DbUpdateException dbEx
                && dbEx.InnerException is Microsoft.Data.Sqlite.SqliteException innerSqlite
                && TransientSqliteErrorCodes.Contains(innerSqlite.SqliteErrorCode))
                return true;

            // Under extreme concurrency on a shared SQLite connection, the driver can
            // throw ANY exception type from corrupted internal state (e.g., list index
            // out of range in RemoveCommand, null reference in Close, transaction/connection
            // mismatch). Check if the exception originated from the SQLite driver or
            // EF Core data layer by inspecting the stack trace.
            if (OriginatesFromSqliteStack(current))
                return true;
        }

        return false;
    }

    private static bool OriginatesFromSqliteStack(Exception ex)
    {
        // SqliteException is already handled above by error code; skip it here
        if (ex is Microsoft.Data.Sqlite.SqliteException)
            return false;

        var trace = ex.StackTrace;
        if (trace is null) return false;

        return trace.Contains("Microsoft.Data.Sqlite", StringComparison.Ordinal)
            || trace.Contains("Microsoft.EntityFrameworkCore.Storage", StringComparison.Ordinal)
            || trace.Contains("Microsoft.EntityFrameworkCore.Query", StringComparison.Ordinal)
            || trace.Contains("Microsoft.EntityFrameworkCore.Update", StringComparison.Ordinal);
    }
}
