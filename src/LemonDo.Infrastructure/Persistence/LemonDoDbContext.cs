namespace LemonDo.Infrastructure.Persistence;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Common;
using LemonDo.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

public sealed class LemonDoDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher? _eventDispatcher;

    public LemonDoDbContext(DbContextOptions<LemonDoDbContext> options, IDomainEventDispatcher? eventDispatcher = null)
        : base(options)
    {
        _eventDispatcher = eventDispatcher;
    }

    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
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

        // Collect domain events before save (entities might be detached after)
        var domainEvents = ChangeTracker.Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .SelectMany(e =>
            {
                var events = e.DomainEvents.ToList();
                e.ClearDomainEvents();
                return events;
            })
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch events after save (events represent committed facts)
        if (_eventDispatcher is not null && domainEvents.Count > 0)
            await _eventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        return result;
    }
}
