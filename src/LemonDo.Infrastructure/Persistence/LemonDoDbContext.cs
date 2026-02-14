namespace LemonDo.Infrastructure.Persistence;

using LemonDo.Application.Common;
using LemonDo.Domain.Tasks.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class LemonDoDbContext(DbContextOptions<LemonDoDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Board> Boards => Set<Board>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LemonDoDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite doesn't natively support DateTimeOffset. Store as ISO 8601 strings
        // which sort lexicographically correctly and support ORDER BY.
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<string>();
        configurationBuilder.Properties<DateTimeOffset?>()
            .HaveConversion<string>();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in entries)
        {
            if (entry.Metadata.FindProperty("UpdatedAt") is not null)
                entry.Property("UpdatedAt").CurrentValue = now;

            if (entry.State == EntityState.Added && entry.Metadata.FindProperty("CreatedAt") is not null)
                entry.Property("CreatedAt").CurrentValue = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
