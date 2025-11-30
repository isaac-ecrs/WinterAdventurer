using Microsoft.EntityFrameworkCore;
using WinterAdventurer.Data;
using Microsoft.Extensions.Logging;

namespace WinterAdventurer.Services;

public interface ITagService
{
    Task<List<Tag>> GetAllTagsAsync();
    Task<Tag?> GetTagByNameAsync(string name);
    Task<Tag> CreateTagAsync(string name, string? color = null);
    Task<bool> DeleteTagAsync(string name);
    Task<List<Tag>> GetTagsForLocationAsync(string locationName);
    Task AssignTagToLocationAsync(string locationName, string tagName);
    Task<bool> RemoveTagFromLocationAsync(string locationName, string tagName);
}

public class TagService : ITagService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TagService> _logger;

    public TagService(ApplicationDbContext context, ILogger<TagService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Tag>> GetAllTagsAsync()
    {
        _logger.LogDebug("Loading all tags from database");
        var tags = await _context.Tags
            .OrderBy(t => t.Name)
            .ToListAsync();
        _logger.LogDebug("Loaded {Count} tags", tags.Count);
        return tags;
    }

    public async Task<Tag?> GetTagByNameAsync(string name)
    {
        _logger.LogDebug("Looking up tag by name: '{Name}'", name);
        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Name == name);
        _logger.LogDebug("Tag lookup result: {Found}", tag != null ? "FOUND" : "NOT FOUND");
        return tag;
    }

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
