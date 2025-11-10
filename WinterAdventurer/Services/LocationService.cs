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
    Task<string?> GetWorkshopLocationMappingAsync(string workshopName);
    Task SaveWorkshopLocationMappingAsync(string workshopName, string locationName);
    Task<Dictionary<string, string>> GetAllWorkshopLocationMappingsAsync();
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

    public async Task<string?> GetWorkshopLocationMappingAsync(string workshopName)
    {
        _logger.LogDebug("Looking up location mapping for workshop: '{WorkshopName}'", workshopName);

        var mapping = await _context.WorkshopLocationMappings
            .FirstOrDefaultAsync(m => m.WorkshopName == workshopName);

        var result = mapping?.LocationName;
        _logger.LogDebug("Workshop '{WorkshopName}' mapping result: {Location}",
            workshopName, result ?? "NOT FOUND");

        return result;
    }

    public async Task SaveWorkshopLocationMappingAsync(string workshopName, string locationName)
    {
        _logger.LogInformation("Saving workshop-location mapping: '{WorkshopName}' -> '{LocationName}'",
            workshopName, locationName);

        var existing = await _context.WorkshopLocationMappings
            .FirstOrDefaultAsync(m => m.WorkshopName == workshopName);

        if (existing != null)
        {
            existing.LocationName = locationName;
            existing.LastUpdated = DateTime.UtcNow;
            _logger.LogDebug("Updated existing mapping for '{WorkshopName}'", workshopName);
        }
        else
        {
            var mapping = new WorkshopLocationMapping
            {
                WorkshopName = workshopName,
                LocationName = locationName
            };
            _context.WorkshopLocationMappings.Add(mapping);
            _logger.LogDebug("Created new mapping for '{WorkshopName}'", workshopName);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("MAPPING SAVED: '{WorkshopName}' -> '{LocationName}'",
            workshopName, locationName);
    }

    public async Task<Dictionary<string, string>> GetAllWorkshopLocationMappingsAsync()
    {
        _logger.LogDebug("Loading all workshop-location mappings from database");

        var mappings = await _context.WorkshopLocationMappings
            .ToDictionaryAsync(m => m.WorkshopName, m => m.LocationName);

        _logger.LogDebug("Loaded {Count} workshop-location mappings", mappings.Count);
        return mappings;
    }
}
