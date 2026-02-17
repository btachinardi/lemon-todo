namespace LemonDo.Domain.Identity.Repositories;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Entities;
using LemonDo.Domain.Identity.ValueObjects;

/// <summary>
/// Repository for persisting and querying <see cref="User"/> aggregates.
/// The implementation handles protected data encryption transparently: during <see cref="AddAsync"/>,
/// the pre-encrypted fields are stored directly; during reads, only redacted values are loaded.
/// </summary>
public interface IUserRepository
{
    /// <summary>Returns the user with the given ID, or <c>null</c> if not found. Protected data is redacted.</summary>
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);

    /// <summary>
    /// Persists a new user. The <paramref name="email"/> and <paramref name="displayName"/> EncryptedFields
    /// contain the pre-encrypted values, hashes, and redacted forms — the entity itself only holds redacted strings.
    /// </summary>
    Task AddAsync(User user, EncryptedField email, EncryptedField displayName, CancellationToken ct = default);

    /// <summary>Marks an existing user as modified for the next unit-of-work commit.</summary>
    Task UpdateAsync(User user, CancellationToken ct = default);
}
