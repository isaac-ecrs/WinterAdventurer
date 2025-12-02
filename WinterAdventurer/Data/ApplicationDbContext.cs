using Microsoft.EntityFrameworkCore;

namespace WinterAdventurer.Data;

/// <summary>
/// Entity Framework Core database context for managing location and tag data in the WinterAdventurer system.
/// </summary>
/// <remarks>
/// This context provides access to locations, tags, workshop-location mappings, and time slots.
/// It configures entity relationships including a many-to-many relationship between locations and tags.
/// </remarks>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options, including connection string and provider configuration.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the entity set for physical locations where workshops can be held.
    /// </summary>
    /// <remarks>
    /// Each location has a unique name and can be associated with multiple tags.
    /// Examples include "Dining Room", "Chapel A", "Rec Hall", etc.
    /// </remarks>
    public DbSet<Location> Locations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the entity set for mappings between workshop names and their assigned locations.
    /// </summary>
    /// <remarks>
    /// Stores persistent location assignments for workshops, keyed by workshop name.
    /// Each workshop name can have only one location mapping.
    /// </remarks>
    public DbSet<WorkshopLocationMapping> WorkshopLocationMappings { get; set; } = null!;

    /// <summary>
    /// Gets or sets the entity set for time slot definitions used in the scheduling system.
    /// </summary>
    /// <remarks>
    /// Time slots define the periods when workshops occur (e.g., "Morning", "Afternoon").
    /// </remarks>
    public DbSet<TimeSlot> TimeSlots { get; set; } = null!;

    /// <summary>
    /// Gets or sets the entity set for tags that can be applied to locations.
    /// </summary>
    /// <remarks>
    /// Tags provide categorical labels for locations (e.g., "Downstairs") with associated colors.
    /// Each tag has a unique name and can be associated with multiple locations.
    /// </remarks>
    public DbSet<Tag> Tags { get; set; } = null!;

    /// <summary>
    /// Configures the database schema and entity relationships using the Entity Framework model builder.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the database model.</param>
    /// <remarks>
    /// This method configures:
    /// <list type="bullet">
    /// <item><description>Location entity: Unique name index and many-to-many relationship with tags via LocationTag join table</description></item>
    /// <item><description>Tag entity: Unique name index to prevent duplicate tag names</description></item>
    /// <item><description>WorkshopLocationMapping entity: Unique workshop name index to ensure one-to-one workshop-location mapping</description></item>
    /// </list>
    /// </remarks>
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
