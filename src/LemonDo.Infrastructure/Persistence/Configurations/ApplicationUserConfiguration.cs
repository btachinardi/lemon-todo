namespace LemonDo.Infrastructure.Persistence.Configurations;

using LemonDo.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core configuration for <see cref="ApplicationUser"/>.
/// Identity handles its own schema — this configuration exists only for
/// any future custom column additions to <c>AspNetUsers</c>.
/// </summary>
public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    /// <summary>No custom columns — Identity manages the schema.</summary>
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // ApplicationUser has no custom properties.
        // All user profile data is on the domain User entity (Users table).
    }
}
