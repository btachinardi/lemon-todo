namespace LemonDo.Infrastructure.Persistence.Configurations;

using LemonDo.Domain.Administration;
using LemonDo.Domain.Administration.Entities;
using LemonDo.Domain.Administration.ValueObjects;
using LemonDo.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>EF Core configuration for the <see cref="AuditEntry"/> entity.</summary>
public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsValueObject<AuditEntryId>();

        builder.Property(e => e.ActorId);

        builder.Property(e => e.Action)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ResourceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ResourceId)
            .HasMaxLength(200);

        builder.Property(e => e.Details)
            .HasMaxLength(4000);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt).IsRequired();

        // Index for common query patterns
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.ActorId);
        builder.HasIndex(e => e.Action);
    }
}
