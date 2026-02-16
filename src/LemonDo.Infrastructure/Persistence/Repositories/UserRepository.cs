namespace LemonDo.Infrastructure.Persistence.Repositories;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Entities;
using LemonDo.Domain.Identity.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository for persisting and querying domain <see cref="User"/> aggregates.
/// Handles protected data encryption transparently: during <see cref="AddAsync"/>, plaintext VOs
/// are encrypted into shadow properties; during reads, only redacted values are loaded.
/// </summary>
public sealed class UserRepository(
    LemonDoDbContext dbContext,
    IFieldEncryptionService encryptionService) : IUserRepository
{
    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default)
    {
        // Tracked — allows subsequent Deactivate()/Reactivate() + SaveChanges
        return await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(User user, Email email, DisplayName displayName, CancellationToken ct = default)
    {
        dbContext.Users.Add(user);

        // Set shadow properties with encrypted protected data
        var entry = dbContext.Entry(user);
        entry.Property("EmailHash").CurrentValue = ProtectedDataHasher.HashEmail(email.Value);
        entry.Property("EncryptedEmail").CurrentValue = encryptionService.Encrypt(email.Value);
        entry.Property("EncryptedDisplayName").CurrentValue = encryptionService.Encrypt(displayName.Value);

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
