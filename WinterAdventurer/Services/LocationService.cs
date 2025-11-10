using Microsoft.EntityFrameworkCore;
using WinterAdventurer.Data;
using Microsoft.Extensions.Logging;

namespace WinterAdventurer.Services;

public interface ILocationService
{
    Task<List<string>> GetAllLocationNamesAsync();
    Task<Location?> GetLocationByNameAsync(string name);
    Task<Location> AddOrGetLocationAsync(string name);
    Task<bool> DeleteLocationAsync(string name);
}

public class LocationService : ILocationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LocationService> _logger;

    public LocationService(ApplicationDbContext context, ILogger<LocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<string>> GetAllLocationNamesAsync()
    {
        _logger.LogDebug("Loading all location names from database");
        var locations = await _context.Locations
            .OrderBy(l => l.Name)
            .Select(l => l.Name)
            .ToListAsync();
        _logger.LogDebug("Loaded {Count} locations", locations.Count);
        return locations;
    }

    public async Task<Location?> GetLocationByNameAsync(string name)
    {
        _logger.LogDebug("Looking up location by name: '{Name}'", name);
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Name == name);
        _logger.LogDebug("Location lookup result: {Found}", location != null ? "FOUND" : "NOT FOUND");
        return location;
    }

    public async Task<Location> AddOrGetLocationAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Attempted to add empty/null location name");
            throw new ArgumentException("Location name cannot be empty", nameof(name));
        }

        _logger.LogInformation("AddOrGetLocation called for: '{Name}'", name);

        var existing = await GetLocationByNameAsync(name);
        if (existing != null)
        {
            _logger.LogDebug("Location '{Name}' already exists with ID {Id}", name, existing.Id);
            return existing;
        }

        var location = new Location { Name = name };
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();
        _logger.LogInformation("NEW LOCATION SAVED: '{Name}' (ID: {Id})", name, location.Id);
        return location;
    }

    public async Task<bool> DeleteLocationAsync(string name)
    {
        _logger.LogInformation("DeleteLocation called for: '{Name}'", name);

        var location = await GetLocationByNameAsync(name);
        if (location == null)
        {
            _logger.LogWarning("Cannot delete location '{Name}': Not found", name);
            return false;
        }

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync();
        _logger.LogInformation("LOCATION DELETED: '{Name}' (ID: {Id})", name, location.Id);
        return true;
    }
}
