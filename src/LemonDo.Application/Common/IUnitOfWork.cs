namespace LemonDo.Application.Common;

/// <summary>
/// Abstracts the persistence transaction boundary. The implementation auto-sets timestamps,
/// collects domain events from tracked entities, and dispatches them after the commit.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
