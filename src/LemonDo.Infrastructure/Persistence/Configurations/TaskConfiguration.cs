namespace LemonDo.Infrastructure.Persistence.Configurations;

using LemonDo.Domain.Tasks.ValueObjects;
using LemonDo.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>EF Core configuration for the Task aggregate, including the owned <c>TaskTags</c> table.</summary>
public sealed class TaskConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    /// <summary>
    /// Configures the Task entity mapping to the Tasks table with owned collection for
    /// tags (TaskTags table). Uses value object conversions for strongly-typed IDs and
    /// validates Title/Description max lengths.
    /// </summary>
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).IsValueObject();
        builder.Property(t => t.Title).IsValueObject(TaskTitle.MaxLength);
        builder.Property(t => t.Description).IsNullableValueObject(TaskDescription.MaxLength);

        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.OwnerId).IsValueObject().IsRequired();

        builder.Property(t => t.IsArchived);
        builder.Property(t => t.IsDeleted);
        builder.Property(t => t.DueDate);
        builder.Property(t => t.CompletedAt);
        builder.Property(t => t.CreatedAt);
        builder.Property(t => t.UpdatedAt);

        builder.Ignore(t => t.DomainEvents);

        builder.OwnsMany(t => t.Tags, tagBuilder =>
        {
            tagBuilder.ToTable("TaskTags");
            tagBuilder.WithOwner().HasForeignKey("TaskId");
            tagBuilder.Property(t => t.Value)
                .HasColumnName("Value")
                .HasMaxLength(Tag.MaxLength)
                .IsRequired();
            tagBuilder.HasKey("TaskId", "Value");
        });

        builder.Navigation(t => t.Tags).HasField("_tags");

        builder.HasIndex(t => t.OwnerId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.IsDeleted);
    }
}
