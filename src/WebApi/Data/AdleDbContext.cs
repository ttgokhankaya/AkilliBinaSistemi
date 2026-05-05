using Microsoft.EntityFrameworkCore;

namespace WebApi.Data;

public class AdleDbContext : DbContext
{
    public AdleDbContext(DbContextOptions<AdleDbContext> options) : base(options) { }

    public DbSet<Area> Areas => Set<Area>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Area>(e =>
        {
            e.ToTable("Areas", "public");
            e.HasKey(x => x.ID);
        });
    }
}
