namespace LemonDo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LemonDoDbContext>
{
    public LemonDoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LemonDoDbContext>();
        optionsBuilder.UseSqlite("Data Source=lemondo.db");

        return new LemonDoDbContext(optionsBuilder.Options);
    }
}
