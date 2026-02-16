namespace LemonDo.Infrastructure.Persistence.Configurations;

using LemonDo.Domain.Identity.Entities;
using LemonDo.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core configuration for the domain <see cref="User"/> entity.
/// Maps to the <c>Users</c> table with shadow properties for protected data storage:
/// <list type="bullet">
///   <item><c>EmailHash</c> — SHA-256 hash for exact-match lookups (unique index).</item>
///   <item><c>EncryptedEmail</c> — AES-256-GCM encrypted email (source of truth).</item>
///   <item><c>EncryptedDisplayName</c> — AES-256-GCM encrypted display name (source of truth).</item>
/// </list>
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>Configures the Users table with redacted properties and encrypted shadow properties.</summary>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).IsValueObject();

        builder.Property(u => u.RedactedEmail)
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(u => u.RedactedDisplayName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.IsDeactivated)
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAt);
        builder.Property(u => u.UpdatedAt);

        // Shadow properties for protected data — not exposed on the domain entity
        builder.Property<string>("EmailHash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property<string>("EncryptedEmail")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property<string>("EncryptedDisplayName")
            .HasMaxLength(500)
            .IsRequired();

        builder.Ignore(u => u.DomainEvents);

        // Unique index on email hash for login lookups
        builder.HasIndex("EmailHash").IsUnique();
    }
}
