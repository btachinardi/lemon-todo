namespace LemonDo.Migrations.Sqlite;

using LemonDo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Design-time factory for <c>dotnet ef</c> commands targeting the SQLite provider.
/// Produces migrations in the <c>LemonDo.Migrations.Sqlite</c> assembly.
/// </summary>
public sealed class DesignTimeSqliteDbContextFactory : IDesignTimeDbContextFactory<LemonDoDbContext>
{
    /// <inheritdoc />
    public LemonDoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LemonDoDbContext>();
        optionsBuilder.UseSqlite(
            "Data Source=lemondo.db",
            b => b.MigrationsAssembly("LemonDo.Migrations.Sqlite"));

        return new LemonDoDbContext(optionsBuilder.Options);
    }
}
