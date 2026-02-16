namespace LemonDo.Migrations.SqlServer;

using LemonDo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Design-time factory for <c>dotnet ef</c> commands targeting the SQL Server provider.
/// Produces migrations in the <c>LemonDo.Migrations.SqlServer</c> assembly.
/// </summary>
public sealed class DesignTimeSqlServerDbContextFactory : IDesignTimeDbContextFactory<LemonDoDbContext>
{
    /// <inheritdoc />
    public LemonDoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LemonDoDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=LemonDo;Trusted_Connection=True;TrustServerCertificate=True;",
            b => b.MigrationsAssembly("LemonDo.Migrations.SqlServer"));

        return new LemonDoDbContext(optionsBuilder.Options);
    }
}
