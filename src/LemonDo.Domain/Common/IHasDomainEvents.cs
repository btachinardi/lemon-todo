namespace LemonDo.Domain.Common;

/// <summary>
/// Marks an entity as capable of raising domain events.
/// Events are collected during mutations and dispatched after <see cref="Application.Common.IUnitOfWork.SaveChangesAsync"/>
/// commits the transaction.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<DomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
