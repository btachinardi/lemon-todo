namespace LemonDo.Application.Common;

/// <summary>
/// Abstracts the persistence transaction boundary. The implementation auto-sets timestamps,
/// collects domain events from tracked entities, and dispatches them after the commit.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Commits all pending changes to the database and dispatches domain events. Returns the number of affected rows.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
