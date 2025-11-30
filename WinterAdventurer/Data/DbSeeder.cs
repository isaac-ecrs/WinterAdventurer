using Microsoft.EntityFrameworkCore;

namespace WinterAdventurer.Data;

public static class DbSeeder
{
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

    private static readonly (string Name, string Color)[] DefaultTags = new[]
    {
        ("Downstairs", "#FF5722")  // Orange color for downstairs tag
    };

    public static async Task SeedDefaultDataAsync(ApplicationDbContext context, ILogger logger)
    {
        await SeedDefaultTagsAsync(context, logger);
        await SeedDefaultLocationsAsync(context, logger);
    }

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
