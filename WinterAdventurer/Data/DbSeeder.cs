using Microsoft.EntityFrameworkCore;

namespace WinterAdventurer.Data;

/// <summary>
/// Provides database seeding functionality to populate initial location and tag data.
/// </summary>
/// <remarks>
/// This class ensures the database contains a standard set of locations and tags when the application starts.
/// Seeding operations are idempotent - they only add data that doesn't already exist.
/// </remarks>
public static class DbSeeder
{
    /// <summary>
    /// Default locations to seed into the database on first run.
    /// </summary>
    /// <remarks>
    /// Each tuple contains the location name and an optional tag name to associate with that location.
    /// These represent common workshop venues at the event facility.
    /// </remarks>
    private static readonly (string Name, string? TagName)[] DefaultLocations = new[]
    {
        ("Dining Room", null),
        ("Martin Room", null),
        ("Chapel A", null),
        ("Craft Room", "Downstairs"),
        ("Elm Room", null),
        ("Rec Hall", "Downstairs"),
        ("Library", null)
    };

    /// <summary>
    /// Default tags to seed into the database on first run.
    /// </summary>
    /// <remarks>
    /// Each tuple contains the tag name and a hex color code for visual representation in the UI.
    /// Tags provide categorical labels for locations (e.g., accessibility information).
    /// </remarks>
    private static readonly (string Name, string Color)[] DefaultTags = new[]
    {
        ("Downstairs", "#FF5722")  // Orange color for downstairs tag
    };

    /// <summary>
    /// Seeds the database with default tags and locations if they don't already exist.
    /// </summary>
    /// <param name="context">The database context to seed data into.</param>
    /// <param name="logger">Logger for recording seeding operations.</param>
    /// <returns>A task representing the asynchronous seeding operation.</returns>
    /// <remarks>
    /// This method is idempotent and safe to call on every application startup.
    /// It first seeds tags, then locations, and finally assigns tag-location relationships.
    /// </remarks>
    public static async Task SeedDefaultDataAsync(ApplicationDbContext context, ILogger logger)
    {
        await SeedDefaultTagsAsync(context, logger);
        await SeedDefaultLocationsAsync(context, logger);
    }

    /// <summary>
    /// Seeds default tags into the database, skipping any that already exist.
    /// </summary>
    /// <param name="context">The database context to seed tags into.</param>
    /// <param name="logger">Logger for recording the seeding operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Checks existing tag names before adding new tags to prevent duplicates.
    /// Logs the number and names of newly seeded tags.
    /// </remarks>
    private static async Task SeedDefaultTagsAsync(ApplicationDbContext context, ILogger logger)
    {
        // Get existing tag names
        var existingTagNames = await context.Tags
            .Select(t => t.Name)
            .ToListAsync();

        // Find tags that don't exist yet
        var tagsToAdd = DefaultTags
            .Where(tag => !existingTagNames.Contains(tag.Name))
            .ToList();

        if (tagsToAdd.Count == 0)
        {
            logger.LogInformation("All default tags already exist in database");
            return;
        }

        // Create new tag entities
        var newTags = tagsToAdd.Select(tag => new Tag
        {
            Name = tag.Name,
            Color = tag.Color,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        // Add and save
        context.Tags.AddRange(newTags);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} default tags: {Tags}",
            newTags.Count,
            string.Join(", ", tagsToAdd.Select(t => t.Name)));
    }

    /// <summary>
    /// Seeds default locations into the database, skipping any that already exist.
    /// </summary>
    /// <param name="context">The database context to seed locations into.</param>
    /// <param name="logger">Logger for recording the seeding operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Checks existing location names before adding new locations to prevent duplicates.
    /// After seeding locations, calls <see cref="AssignDefaultTagsToLocationsAsync"/> to establish tag relationships.
    /// Logs the number and names of newly seeded locations.
    /// </remarks>
    private static async Task SeedDefaultLocationsAsync(ApplicationDbContext context, ILogger logger)
    {
        // Get existing location names
        var existingLocationNames = await context.Locations
            .Select(l => l.Name)
            .ToListAsync();

        // Find locations that don't exist yet
        var locationsToAdd = DefaultLocations
            .Where(loc => !existingLocationNames.Contains(loc.Name))
            .ToList();

        if (locationsToAdd.Count == 0)
        {
            logger.LogInformation("All default locations already exist in database");
        }
        else
        {
            // Create new location entities
            var newLocations = locationsToAdd.Select(loc => new Location
            {
                Name = loc.Name,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            // Add and save locations first
            context.Locations.AddRange(newLocations);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} default locations: {Locations}",
                newLocations.Count,
                string.Join(", ", locationsToAdd.Select(l => l.Name)));
        }

        // Assign tags to locations
        await AssignDefaultTagsToLocationsAsync(context, logger);
    }

    /// <summary>
    /// Assigns default tags to their associated locations based on the <see cref="DefaultLocations"/> configuration.
    /// </summary>
    /// <param name="context">The database context containing locations and tags.</param>
    /// <param name="logger">Logger for recording tag assignment operations.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Establishes many-to-many relationships between locations and tags.
    /// Only creates relationships that don't already exist, making this operation idempotent.
    /// Logs the number of new tag assignments created.
    /// </remarks>
    private static async Task AssignDefaultTagsToLocationsAsync(ApplicationDbContext context, ILogger logger)
    {
        // Load all locations and tags with their relationships
        var locations = await context.Locations
            .Include(l => l.Tags)
            .ToListAsync();

        var tags = await context.Tags.ToListAsync();

        int assignmentsAdded = 0;

        foreach (var (locationName, tagName) in DefaultLocations.Where(l => l.TagName != null))
        {
            var location = locations.FirstOrDefault(l => l.Name == locationName);
            var tag = tags.FirstOrDefault(t => t.Name == tagName);

            if (location != null && tag != null)
            {
                // Check if tag is already assigned
                if (!location.Tags.Any(t => t.Id == tag.Id))
                {
                    location.Tags.Add(tag);
                    assignmentsAdded++;
                }
            }
        }

        if (assignmentsAdded > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Assigned {Count} default tags to locations", assignmentsAdded);
        }
        else
        {
            logger.LogInformation("All default tag assignments already exist");
        }
    }
}
