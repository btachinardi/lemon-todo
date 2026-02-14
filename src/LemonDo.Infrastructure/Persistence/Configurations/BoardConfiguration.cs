namespace LemonDo.Infrastructure.Persistence.Configurations;

using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.ToTable("Boards");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasConversion(id => id.Value, guid => BoardId.From(guid));

        builder.Property(b => b.Name)
            .HasConversion(n => n.Value, v => BoardName.Create(v).Value)
            .HasMaxLength(BoardName.MaxLength)
            .IsRequired();

        builder.Property(b => b.OwnerId)
            .HasConversion(id => id.Value, guid => new UserId(guid))
            .IsRequired();

        builder.Property(b => b.CreatedAt);
        builder.Property(b => b.UpdatedAt);

        builder.Ignore(b => b.DomainEvents);

        builder.OwnsMany(b => b.Columns, columnBuilder =>
        {
            columnBuilder.ToTable("Columns");

            columnBuilder.HasKey(c => c.Id);
            columnBuilder.Property(c => c.Id)
                .HasConversion(id => id.Value, guid => ColumnId.From(guid));

            columnBuilder.Property(c => c.Name)
                .HasConversion(n => n.Value, v => ColumnName.Create(v).Value)
                .HasMaxLength(ColumnName.MaxLength)
                .IsRequired();

            columnBuilder.Property(c => c.Position);
            columnBuilder.Property(c => c.WipLimit);

            columnBuilder.WithOwner().HasForeignKey("BoardId");

            columnBuilder.Ignore(c => c.DomainEvents);
            columnBuilder.Ignore(c => c.CreatedAt);
            columnBuilder.Ignore(c => c.UpdatedAt);
        });

        builder.Navigation(b => b.Columns).HasField("_columns");

        builder.HasIndex(b => b.OwnerId);
    }
}
