namespace LemonDo.Infrastructure.Resilience;

/// <summary>
/// Detects transient SQLite errors anywhere in an exception chain.
/// Used by <see cref="TransientFaultRetryPolicy"/> to decide whether an
/// operation should be retried.
/// </summary>
/// <remarks>
/// SQLite in-memory databases share a single physical connection. Under concurrent
/// access, operations can fail with transient errors that succeed on retry:
///   - Code 1 (SQLITE_ERROR): "cannot start a transaction within a transaction", "SQL logic error"
///   - Code 5 (SQLITE_BUSY): database file locked / cannot execute due to active statements
///   - Code 6 (SQLITE_LOCKED): table in use by another connection
///   - InvalidOperationException: nested transactions, pending local transactions
/// These errors are inherent to SQLite's single-writer architecture and are NOT bugs.
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

            if (current is InvalidOperationException ioe && IsSqliteConnectionStateError(ioe))
                return true;

            // EF Core wraps SQLite errors in DbUpdateException
            if (current is Microsoft.EntityFrameworkCore.DbUpdateException dbEx
                && dbEx.InnerException is Microsoft.Data.Sqlite.SqliteException innerSqlite
                && TransientSqliteErrorCodes.Contains(innerSqlite.SqliteErrorCode))
                return true;
        }

        return false;
    }

    private static bool IsSqliteConnectionStateError(InvalidOperationException ex)
    {
        var msg = ex.Message;
        return msg.Contains("nested transaction", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("SqliteConnection does not support", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("pending local transaction", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("Execute requires the command to have a transaction", StringComparison.OrdinalIgnoreCase);
    }
}
