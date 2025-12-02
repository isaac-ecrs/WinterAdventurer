using Microsoft.EntityFrameworkCore;
using WinterAdventurer.Data;
using Microsoft.Extensions.Logging;

namespace WinterAdventurer.Services;

/// <summary>
/// Provides tag management services for location categorization and organization.
/// Tags enable flexible classification of locations for filtering and visual organization.
/// </summary>
public interface ITagService
{
    /// <summary>
    /// Retrieves all tags from the database in alphabetical order by name.
    /// </summary>
    /// <returns>A list of all tag entities sorted alphabetically.</returns>
    Task<List<Tag>> GetAllTagsAsync();

    /// <summary>
    /// Retrieves a specific tag by its name.
    /// </summary>
    /// <param name="name">The name of the tag to retrieve.</param>
    /// <returns>The tag entity if found; otherwise, null.</returns>
    Task<Tag?> GetTagByNameAsync(string name);

    /// <summary>
    /// Creates a new tag with the specified name and optional color.
    /// </summary>
    /// <param name="name">The name of the tag to create.</param>
    /// <param name="color">Optional color code for the tag (e.g., hex color or CSS color name).</param>
    /// <returns>The newly created tag entity.</returns>
    /// <exception cref="ArgumentException">Thrown when the tag name is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a tag with the same name already exists.</exception>
    Task<Tag> CreateTagAsync(string name, string? color = null);

    /// <summary>
    /// Deletes a tag from the database by name.
    /// </summary>
    /// <param name="name">The name of the tag to delete.</param>
    /// <returns>True if the tag was deleted; false if it was not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tag is still assigned to one or more locations.</exception>
    Task<bool> DeleteTagAsync(string name);

    /// <summary>
    /// Retrieves all tags assigned to a specific location.
    /// </summary>
    /// <param name="locationName">The name of the location to query.</param>
    /// <returns>A list of tags assigned to the location, sorted alphabetically. Returns an empty list if the location is not found.</returns>
    Task<List<Tag>> GetTagsForLocationAsync(string locationName);

    /// <summary>
    /// Assigns a tag to a location, creating a many-to-many relationship.
    /// If the tag is already assigned to the location, the operation is idempotent and no error occurs.
    /// </summary>
    /// <param name="locationName">The name of the location to tag.</param>
    /// <param name="tagName">The name of the tag to assign.</param>
    /// <exception cref="InvalidOperationException">Thrown when the location or tag is not found.</exception>
    Task AssignTagToLocationAsync(string locationName, string tagName);

    /// <summary>
    /// Removes a tag assignment from a location.
    /// </summary>
    /// <param name="locationName">The name of the location to update.</param>
    /// <param name="tagName">The name of the tag to remove.</param>
    /// <returns>True if the tag was removed; false if the location or tag assignment was not found.</returns>
    Task<bool> RemoveTagFromLocationAsync(string locationName, string tagName);
}

/// <summary>
/// Implementation of tag management services using Entity Framework Core.
/// Provides CRUD operations for tags and manages tag-to-location relationships,
/// with comprehensive logging and validation.
/// </summary>
public class TagService : ITagService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TagService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagService"/> class.
    /// </summary>
    /// <param name="context">The Entity Framework database context for data access.</param>
    /// <param name="logger">The logger instance for diagnostic logging.</param>
    public TagService(ApplicationDbContext context, ILogger<TagService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<Tag>> GetAllTagsAsync()
    {
        _logger.LogDebug("Loading all tags from database");
        var tags = await _context.Tags
            .OrderBy(t => t.Name)
            .ToListAsync();
        _logger.LogDebug("Loaded {Count} tags", tags.Count);
        return tags;
    }

    /// <inheritdoc />
    public async Task<Tag?> GetTagByNameAsync(string name)
    {
        _logger.LogDebug("Looking up tag by name: '{Name}'", name);
        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Name == name);
        _logger.LogDebug("Tag lookup result: {Found}", tag != null ? "FOUND" : "NOT FOUND");
        return tag;
    }

    /// <inheritdoc />
    public async Task<Tag> CreateTagAsync(string name, string? color = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Attempted to create tag with empty/null name");
            throw new ArgumentException("Tag name cannot be empty", nameof(name));
        }

        _logger.LogInformation("CreateTag called for: '{Name}' (color: {Color})", name, color ?? "none");

        var existing = await GetTagByNameAsync(name);
        if (existing != null)
        {
            _logger.LogWarning("Tag '{Name}' already exists with ID {Id}", name, existing.Id);
            throw new InvalidOperationException($"Tag '{name}' already exists");
        }

        var tag = new Tag
        {
            Name = name,
            Color = color,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        _logger.LogInformation("NEW TAG CREATED: '{Name}' (ID: {Id})", name, tag.Id);
        return tag;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTagAsync(string name)
    {
        _logger.LogInformation("DeleteTag called for: '{Name}'", name);

        var tag = await _context.Tags
            .Include(t => t.Locations)
            .FirstOrDefaultAsync(t => t.Name == name);

        if (tag == null)
        {
            _logger.LogWarning("Cannot delete tag '{Name}': Not found", name);
            return false;
        }

        if (tag.Locations.Any())
        {
            _logger.LogWarning("Cannot delete tag '{Name}': Assigned to {Count} locations",
                name, tag.Locations.Count);
            throw new InvalidOperationException(
                $"Cannot delete tag '{name}' - it is assigned to {tag.Locations.Count} location(s)");
        }

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();
        _logger.LogInformation("TAG DELETED: '{Name}' (ID: {Id})", name, tag.Id);
        return true;
    }

    /// <inheritdoc />
    public async Task<List<Tag>> GetTagsForLocationAsync(string locationName)
    {
        _logger.LogDebug("Loading tags for location: '{LocationName}'", locationName);

        var location = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == locationName);

        if (location == null)
        {
            _logger.LogWarning("Location '{LocationName}' not found", locationName);
            return new List<Tag>();
        }

        var tags = location.Tags.OrderBy(t => t.Name).ToList();
        _logger.LogDebug("Location '{LocationName}' has {Count} tags", locationName, tags.Count);
        return tags;
    }

    /// <inheritdoc />
    public async Task AssignTagToLocationAsync(string locationName, string tagName)
    {
        _logger.LogInformation("Assigning tag '{TagName}' to location '{LocationName}'",
            tagName, locationName);

        var location = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == locationName);

        if (location == null)
        {
            _logger.LogWarning("Location '{LocationName}' not found", locationName);
            throw new InvalidOperationException($"Location '{locationName}' not found");
        }

        var tag = await GetTagByNameAsync(tagName);
        if (tag == null)
        {
            _logger.LogWarning("Tag '{TagName}' not found", tagName);
            throw new InvalidOperationException($"Tag '{tagName}' not found");
        }

        if (location.Tags.Any(t => t.Id == tag.Id))
        {
            _logger.LogDebug("Tag '{TagName}' already assigned to '{LocationName}'",
                tagName, locationName);
            return; // Already assigned
        }

        location.Tags.Add(tag);
        await _context.SaveChangesAsync();
        _logger.LogInformation("TAG ASSIGNED: '{TagName}' -> '{LocationName}'",
            tagName, locationName);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveTagFromLocationAsync(string locationName, string tagName)
    {
        _logger.LogInformation("Removing tag '{TagName}' from location '{LocationName}'",
            tagName, locationName);

        var location = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == locationName);

        if (location == null)
        {
            _logger.LogWarning("Location '{LocationName}' not found", locationName);
            return false;
        }

        var tag = location.Tags.FirstOrDefault(t => t.Name == tagName);
        if (tag == null)
        {
            _logger.LogWarning("Tag '{TagName}' not assigned to '{LocationName}'",
                tagName, locationName);
            return false;
        }

        location.Tags.Remove(tag);
        await _context.SaveChangesAsync();
        _logger.LogInformation("TAG REMOVED: '{TagName}' from '{LocationName}'",
            tagName, locationName);
        return true;
    }
}
