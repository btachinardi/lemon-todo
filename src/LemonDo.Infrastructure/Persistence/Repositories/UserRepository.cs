namespace LemonDo.Infrastructure.Persistence.Repositories;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Entities;
using LemonDo.Domain.Identity.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository for persisting and querying domain <see cref="User"/> aggregates.
/// Handles protected data storage transparently: during <see cref="AddAsync"/>, pre-encrypted
/// <see cref="EncryptedField"/> values are stored directly into shadow properties; during reads,
/// only redacted values are loaded.
/// </summary>
public sealed class UserRepository(LemonDoDbContext dbContext) : IUserRepository
{
    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default)
    {
        // Tracked — allows subsequent Deactivate()/Reactivate() + SaveChanges
        return await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(User user, EncryptedField email, EncryptedField displayName, CancellationToken ct = default)
    {
        dbContext.Users.Add(user);

        // Set shadow properties with pre-encrypted protected data from EncryptedField
        var entry = dbContext.Entry(user);
        entry.Property("EmailHash").CurrentValue = email.Hash
            ?? throw new InvalidOperationException("EncryptedField for email must have a hash.");
        entry.Property("EncryptedEmail").CurrentValue = email.Encrypted;
        entry.Property("EncryptedDisplayName").CurrentValue = displayName.Encrypted;

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        // Entity is normally already tracked from GetByIdAsync — change tracker handles the rest.
        // If detached (unusual), re-attach and mark all properties modified.
        if (dbContext.Entry(user).State == EntityState.Detached)
            dbContext.Users.Update(user);

        return Task.CompletedTask;
    }
}
