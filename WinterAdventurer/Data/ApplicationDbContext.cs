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
    public DbSet<Tag> Tags { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Location entity
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();

            // Many-to-many with Tag
            entity.HasMany(l => l.Tags)
                .WithMany(t => t.Locations)
                .UsingEntity<Dictionary<string, object>>(
                    "LocationTag",  // Join table name
                    j => j.HasOne<Tag>().WithMany().HasForeignKey("TagId"),
                    j => j.HasOne<Location>().WithMany().HasForeignKey("LocationId")
                );
        });

        // Configure Tag entity
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();  // Tag names must be unique
        });

        // Configure WorkshopLocationMapping entity
        modelBuilder.Entity<WorkshopLocationMapping>(entity =>
        {
            entity.HasIndex(e => e.WorkshopName).IsUnique();
        });
    }
}
