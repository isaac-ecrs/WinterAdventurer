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
public partial class TagService : ITagService
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
        LogLoadingAllTags();
        var tags = await _context.Tags
            .OrderBy(t => t.Name)
            .ToListAsync();
        LogLoadedTags(tags.Count);
        return tags;
    }

    /// <inheritdoc />
    public async Task<Tag?> GetTagByNameAsync(string name)
    {
        LogLookingUpTagByName(name);
        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Name == name);
        LogTagLookupResult(tag != null ? "FOUND" : "NOT FOUND");
        return tag;
    }

    /// <inheritdoc />
    public async Task<Tag> CreateTagAsync(string name, string? color = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            LogWarningAttemptedToCreateEmptyTag();
            throw new ArgumentException("Tag name cannot be empty", nameof(name));
        }

        LogCreateTagCalled(name, color ?? "none");

        var existing = await GetTagByNameAsync(name);
        if (existing != null)
        {
            LogWarningTagAlreadyExists(name, existing.Id);
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
        LogNewTagCreated(name, tag.Id);
        return tag;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTagAsync(string name)
    {
        LogDeleteTagCalled(name);

        var tag = await _context.Tags
            .Include(t => t.Locations)
            .FirstOrDefaultAsync(t => t.Name == name);

        if (tag == null)
        {
            LogWarningCannotDeleteTagNotFound(name);
            return false;
        }

        if (tag.Locations.Any())
        {
            LogWarningCannotDeleteTagAssignedToLocations(name, tag.Locations.Count);
            throw new InvalidOperationException(
                $"Cannot delete tag '{name}' - it is assigned to {tag.Locations.Count} location(s)");
        }

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();
        LogTagDeleted(name, tag.Id);
        return true;
    }

    /// <inheritdoc />
    public async Task<List<Tag>> GetTagsForLocationAsync(string locationName)
    {
        LogLoadingTagsForLocation(locationName);

        var location = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == locationName);

        if (location == null)
        {
            LogWarningLocationNotFoundInGetTags(locationName);
            return new List<Tag>();
        }

        var tags = location.Tags.OrderBy(t => t.Name).ToList();
        LogLocationHasTags(locationName, tags.Count);
        return tags;
    }

    /// <inheritdoc />
    public async Task AssignTagToLocationAsync(string locationName, string tagName)
    {
        LogAssigningTagToLocation(tagName, locationName);

        var location = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == locationName);

        if (location == null)
        {
            LogWarningLocationNotFoundInAssign(locationName);
            throw new InvalidOperationException($"Location '{locationName}' not found");
        }

        var tag = await GetTagByNameAsync(tagName);
        if (tag == null)
        {
            LogWarningTagNotFound(tagName);
            throw new InvalidOperationException($"Tag '{tagName}' not found");
        }

        if (location.Tags.Any(t => t.Id == tag.Id))
        {
            LogTagAlreadyAssigned(tagName, locationName);
            return; // Already assigned
        }

        location.Tags.Add(tag);
        await _context.SaveChangesAsync();
        LogTagAssigned(tagName, locationName);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveTagFromLocationAsync(string locationName, string tagName)
    {
        LogRemovingTagFromLocation(tagName, locationName);

        var location = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == locationName);

        if (location == null)
        {
            LogWarningLocationNotFoundInRemove(locationName);
            return false;
        }

        var tag = location.Tags.FirstOrDefault(t => t.Name == tagName);
        if (tag == null)
        {
            LogWarningTagNotAssigned(tagName, locationName);
            return false;
        }

        location.Tags.Remove(tag);
        await _context.SaveChangesAsync();
        LogTagRemoved(tagName, locationName);
        return true;
    }

    #region Logging

    // Tag CRUD Operations: 8001-8099
    [LoggerMessage(EventId = 8001, Level = LogLevel.Debug, Message = "Loading all tags from database")]
    partial void LogLoadingAllTags();

    [LoggerMessage(EventId = 8002, Level = LogLevel.Debug, Message = "Loaded {Count} tags")]
    partial void LogLoadedTags(int count);

    [LoggerMessage(EventId = 8003, Level = LogLevel.Debug, Message = "Looking up tag by name: '{Name}'")]
    partial void LogLookingUpTagByName(string name);

    [LoggerMessage(EventId = 8004, Level = LogLevel.Debug, Message = "Tag lookup result: {Found}")]
    partial void LogTagLookupResult(string found);

    [LoggerMessage(EventId = 8005, Level = LogLevel.Warning, Message = "Attempted to create tag with empty/null name")]
    partial void LogWarningAttemptedToCreateEmptyTag();

    [LoggerMessage(EventId = 8006, Level = LogLevel.Information, Message = "CreateTag called for: '{Name}' (color: {Color})")]
    partial void LogCreateTagCalled(string name, string color);

    [LoggerMessage(EventId = 8007, Level = LogLevel.Warning, Message = "Tag '{Name}' already exists with ID {Id}")]
    partial void LogWarningTagAlreadyExists(string name, int id);

    [LoggerMessage(EventId = 8008, Level = LogLevel.Information, Message = "NEW TAG CREATED: '{Name}' (ID: {Id})")]
    partial void LogNewTagCreated(string name, int id);

    [LoggerMessage(EventId = 8009, Level = LogLevel.Information, Message = "DeleteTag called for: '{Name}'")]
    partial void LogDeleteTagCalled(string name);

    [LoggerMessage(EventId = 8010, Level = LogLevel.Warning, Message = "Cannot delete tag '{Name}': Not found")]
    partial void LogWarningCannotDeleteTagNotFound(string name);

    [LoggerMessage(EventId = 8011, Level = LogLevel.Warning, Message = "Cannot delete tag '{Name}': Assigned to {Count} locations")]
    partial void LogWarningCannotDeleteTagAssignedToLocations(string name, int count);

    [LoggerMessage(EventId = 8012, Level = LogLevel.Information, Message = "TAG DELETED: '{Name}' (ID: {Id})")]
    partial void LogTagDeleted(string name, int id);

    // Tag-Location Relationship Operations: 8100-8199
    [LoggerMessage(EventId = 8100, Level = LogLevel.Debug, Message = "Loading tags for location: '{LocationName}'")]
    partial void LogLoadingTagsForLocation(string locationName);

    [LoggerMessage(EventId = 8101, Level = LogLevel.Warning, Message = "Location '{LocationName}' not found")]
    partial void LogWarningLocationNotFoundInGetTags(string locationName);

    [LoggerMessage(EventId = 8102, Level = LogLevel.Debug, Message = "Location '{LocationName}' has {Count} tags")]
    partial void LogLocationHasTags(string locationName, int count);

    [LoggerMessage(EventId = 8103, Level = LogLevel.Information, Message = "Assigning tag '{TagName}' to location '{LocationName}'")]
    partial void LogAssigningTagToLocation(string tagName, string locationName);

    [LoggerMessage(EventId = 8104, Level = LogLevel.Warning, Message = "Location '{LocationName}' not found")]
    partial void LogWarningLocationNotFoundInAssign(string locationName);

    [LoggerMessage(EventId = 8105, Level = LogLevel.Warning, Message = "Tag '{TagName}' not found")]
    partial void LogWarningTagNotFound(string tagName);

    [LoggerMessage(EventId = 8106, Level = LogLevel.Debug, Message = "Tag '{TagName}' already assigned to '{LocationName}'")]
    partial void LogTagAlreadyAssigned(string tagName, string locationName);

    [LoggerMessage(EventId = 8107, Level = LogLevel.Information, Message = "TAG ASSIGNED: '{TagName}' -> '{LocationName}'")]
    partial void LogTagAssigned(string tagName, string locationName);

    [LoggerMessage(EventId = 8108, Level = LogLevel.Information, Message = "Removing tag '{TagName}' from location '{LocationName}'")]
    partial void LogRemovingTagFromLocation(string tagName, string locationName);

    [LoggerMessage(EventId = 8109, Level = LogLevel.Warning, Message = "Location '{LocationName}' not found")]
    partial void LogWarningLocationNotFoundInRemove(string locationName);

    [LoggerMessage(EventId = 8110, Level = LogLevel.Warning, Message = "Tag '{TagName}' not assigned to '{LocationName}'")]
    partial void LogWarningTagNotAssigned(string tagName, string locationName);

    [LoggerMessage(EventId = 8111, Level = LogLevel.Information, Message = "TAG REMOVED: '{TagName}' from '{LocationName}'")]
    partial void LogTagRemoved(string tagName, string locationName);

    #endregion
}
