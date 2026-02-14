namespace LemonDo.Infrastructure.Persistence.Configurations;

using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class BoardTaskConfiguration : IEntityTypeConfiguration<BoardTask>
{
    public void Configure(EntityTypeBuilder<BoardTask> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, guid => BoardTaskId.From(guid));

        builder.Property(t => t.Title)
            .HasConversion(t => t.Value, v => TaskTitle.Create(v).Value)
            .HasMaxLength(TaskTitle.MaxLength)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasConversion(
                d => d != null ? d.Value : null,
                v => v != null ? TaskDescription.Create(v).Value : null)
            .HasMaxLength(TaskDescription.MaxLength);

        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.OwnerId)
            .HasConversion(id => id.Value, guid => new UserId(guid))
            .IsRequired();

        builder.Property(t => t.ColumnId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                guid => guid.HasValue ? ColumnId.From(guid.Value) : null);

        builder.Property(t => t.Position);
        builder.Property(t => t.IsArchived);
        builder.Property(t => t.IsDeleted);
        builder.Property(t => t.DueDate);
        builder.Property(t => t.CompletedAt);
        builder.Property(t => t.CreatedAt);
        builder.Property(t => t.UpdatedAt);

        builder.Ignore(t => t.DomainEvents);

        // Tags as separate table for queryability
        builder.OwnsMany(t => t.Tags, tagBuilder =>
        {
            tagBuilder.ToTable("TaskItemTags");
            tagBuilder.WithOwner().HasForeignKey("BoardTaskId");
            tagBuilder.Property(t => t.Value)
                .HasColumnName("Value")
                .HasMaxLength(Tag.MaxLength)
                .IsRequired();
            tagBuilder.HasKey("BoardTaskId", "Value");
        });

        builder.Navigation(t => t.Tags).HasField("_tags");

        builder.HasIndex(t => t.OwnerId);
        builder.HasIndex(t => t.ColumnId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.IsDeleted);
    }
}
