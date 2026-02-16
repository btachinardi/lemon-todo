namespace LemonDo.Domain.Identity.Repositories;

using LemonDo.Domain.Identity.Entities;
using LemonDo.Domain.Identity.ValueObjects;

/// <summary>
/// Repository for persisting and querying <see cref="User"/> aggregates.
/// The implementation handles PII encryption transparently: during <see cref="AddAsync"/>,
/// the plaintext VOs are encrypted for storage; during reads, only redacted values are loaded.
/// </summary>
public interface IUserRepository
{
    /// <summary>Returns the user with the given ID, or <c>null</c> if not found. PII is redacted.</summary>
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);

    /// <summary>
    /// Persists a new user. The <paramref name="email"/> and <paramref name="displayName"/> VOs
    /// are used to compute the email hash and encrypted values — the entity itself only holds redacted strings.
    /// </summary>
    Task AddAsync(User user, Email email, DisplayName displayName, CancellationToken ct = default);

    /// <summary>Marks an existing user as modified for the next unit-of-work commit.</summary>
    Task UpdateAsync(User user, CancellationToken ct = default);
}
