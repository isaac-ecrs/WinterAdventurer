using Microsoft.EntityFrameworkCore;

namespace WinterAdventurer.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<WorkshopLocationMapping> WorkshopLocationMappings { get; set; } = null!;
    public DbSet<TimeSlot> TimeSlots { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Location entity
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure WorkshopLocationMapping entity
        modelBuilder.Entity<WorkshopLocationMapping>(entity =>
        {
            entity.HasIndex(e => e.WorkshopName).IsUnique();
        });
    }
}
