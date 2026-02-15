namespace LemonDo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>Factory used by EF Core CLI tools (<c>dotnet ef</c>) to create a <see cref="LemonDoDbContext"/> at design time.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LemonDoDbContext>
{
    public LemonDoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LemonDoDbContext>();
        optionsBuilder.UseSqlite("Data Source=lemondo.db");

        return new LemonDoDbContext(optionsBuilder.Options);
    }
}
