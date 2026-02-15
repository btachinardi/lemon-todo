namespace LemonDo.Infrastructure.Persistence;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Common;
using LemonDo.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>
/// EF Core DbContext that also implements <see cref="IUnitOfWork"/>.
/// <see cref="SaveChangesAsync"/> auto-manages <c>CreatedAt</c>/<c>UpdatedAt</c> timestamps
/// and dispatches collected domain events after committing.
/// </summary>
/// <remarks>
/// SQLite does not natively support <see cref="DateTimeOffset"/>; values are stored as ISO 8601
/// strings via <see cref="ConfigureConventions"/> to preserve correct sort order.
/// </remarks>
public sealed class LemonDoDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher? _eventDispatcher;

    /// <summary>
    /// Initializes the DbContext with EF Core options and an optional domain event dispatcher.
    /// When dispatcher is provided, domain events are dispatched automatically after SaveChangesAsync.
    /// </summary>
    public LemonDoDbContext(DbContextOptions<LemonDoDbContext> options, IDomainEventDispatcher? eventDispatcher = null)
        : base(options)
    {
        _eventDispatcher = eventDispatcher;
    }

    /// <summary>Gets the DbSet for Task entities, including tags when queried via repository.</summary>
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    /// <summary>Gets the DbSet for Board entities, including columns and task cards when queried via repository.</summary>
    public DbSet<Board> Boards => Set<Board>();

    /// <summary>Applies all EF Core entity configurations from this assembly.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LemonDoDbContext).Assembly);
    }

    /// <summary>Configures <see cref="DateTimeOffset"/> to be stored as ISO 8601 strings for SQLite compatibility.</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite doesn't natively support DateTimeOffset. Store as ISO 8601 strings
        // which sort lexicographically correctly and support ORDER BY.
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<string>();
        configurationBuilder.Properties<DateTimeOffset?>()
            .HaveConversion<string>();
    }

    /// <summary>
    /// Saves changes to the database with automatic timestamp management (CreatedAt, UpdatedAt)
    /// and domain event dispatching. Events are collected before save and dispatched after
    /// commit to ensure they represent committed facts.
    /// </summary>
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
