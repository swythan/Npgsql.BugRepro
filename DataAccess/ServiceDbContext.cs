using Microsoft.EntityFrameworkCore;

namespace Npgsql.BugRepro.DataAccess;

public class ServiceDbContext(DbContextOptions<ServiceDbContext> options)
    : DbContext(options)
{
    public DbSet<ServiceMetadata> Services { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceMetadata>()
            .HasKey(e => e.Id);

        modelBuilder.Entity<ServiceMetadata>()
            .OwnsMany(
                s => s.Attributes,
                e => e.ToJson());
    }
}
