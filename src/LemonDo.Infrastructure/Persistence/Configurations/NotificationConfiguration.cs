namespace LemonDo.Infrastructure.Persistence.Configurations;

using LemonDo.Domain.Notifications.Entities;
using LemonDo.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>EF Core configuration for the <see cref="Notification"/> entity.</summary>
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    /// <summary>Configures the Notification entity mapping to the Notifications table.</summary>
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).IsValueObject();
        builder.Property(n => n.UserId).IsValueObject().IsRequired();

        builder.Property(n => n.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(n => n.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(n => n.Body)
            .HasMaxLength(1000);

        builder.Property(n => n.IsRead);
        builder.Property(n => n.ReadAt);
        builder.Property(n => n.CreatedAt);
        builder.Property(n => n.UpdatedAt);

        builder.Ignore(n => n.DomainEvents);

        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}
