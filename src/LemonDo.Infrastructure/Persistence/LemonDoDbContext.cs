namespace LemonDo.Infrastructure.Persistence;

using LemonDo.Application.Common;
using LemonDo.Domain.Administration.Entities;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Entities;
using LemonDo.Infrastructure.Events;
using LemonDo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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
public sealed class LemonDoDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork
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

    /// <summary>
    /// Gets the DbSet for domain User entities (profile data, separate from Identity).
    /// Hides <see cref="IdentityDbContext{TUser,TRole,TKey}"/>'s <c>Users</c> which returns Identity's <c>AspNetUsers</c>.
    /// </summary>
    public new DbSet<User> Users => Set<User>();

    /// <summary>Gets the DbSet for refresh tokens used in JWT authentication.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Gets the DbSet for audit trail entries.</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <summary>Applies Identity schema and all EF Core entity configurations from this assembly.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
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
    /// and domain event dispatching. When domain events exist, save and event dispatch are wrapped
    /// in an explicit transaction so that event handler failures trigger a full rollback.
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

        // No events → EF Core's implicit transaction is sufficient
        if (_eventDispatcher is null || domainEvents.Count == 0)
            return await base.SaveChangesAsync(cancellationToken);

        // Caller already manages a transaction → participate without wrapping
        if (Database.CurrentTransaction is not null)
        {
            var innerResult = await base.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(domainEvents, cancellationToken);
            return innerResult;
        }

        // Wrap save + event dispatch in explicit transaction for atomicity
        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(domainEvents, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
