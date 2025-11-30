using Microsoft.EntityFrameworkCore;
using WinterAdventurer.Data;
using Microsoft.Extensions.Logging;

namespace WinterAdventurer.Services;

public interface ILocationService
{
    Task<List<string>> GetAllLocationNamesAsync();
    Task<List<Location>> GetAllLocationsWithTagsAsync();
    Task<Location?> GetLocationByNameAsync(string name);
    Task<Location> AddOrGetLocationAsync(string name);
    Task<bool> DeleteLocationAsync(string name);
    Task<string?> GetWorkshopLocationMappingAsync(string workshopName);
    Task SaveWorkshopLocationMappingAsync(string workshopName, string locationName);
    Task<Dictionary<string, string>> GetAllWorkshopLocationMappingsAsync();

    // TimeSlot operations
    Task<List<TimeSlot>> GetAllTimeSlotsAsync();
    Task<TimeSlot?> GetTimeSlotByIdAsync(string id);
    Task<TimeSlot> SaveTimeSlotAsync(TimeSlot timeSlot);
    Task<bool> DeleteTimeSlotAsync(string id);
    Task ClearAllTimeSlotsAsync();
    Task SaveAllTimeSlotsAsync(List<TimeSlot> timeSlots);
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

    public async Task<List<Location>> GetAllLocationsWithTagsAsync()
    {
        _logger.LogDebug("Loading all locations with tags from database");
        var locations = await _context.Locations
            .Include(l => l.Tags)
            .OrderBy(l => l.Name)
            .ToListAsync();
        _logger.LogDebug("Loaded {Count} locations with tags", locations.Count);
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

    // TimeSlot operations
    public async Task<List<TimeSlot>> GetAllTimeSlotsAsync()
    {
        _logger.LogDebug("Loading all timeslots from database");
        var timeSlots = await _context.TimeSlots.ToListAsync();
        _logger.LogDebug("Loaded {Count} timeslots", timeSlots.Count);
        return timeSlots;
    }

    public async Task<TimeSlot?> GetTimeSlotByIdAsync(string id)
    {
        _logger.LogDebug("Looking up timeslot by ID: '{Id}'", id);
        var timeSlot = await _context.TimeSlots.FirstOrDefaultAsync(t => t.Id == id);
        _logger.LogDebug("TimeSlot lookup result: {Found}", timeSlot != null ? "FOUND" : "NOT FOUND");
        return timeSlot;
    }

    public async Task<TimeSlot> SaveTimeSlotAsync(TimeSlot timeSlot)
    {
        _logger.LogInformation("Saving timeslot: '{Label}' (ID: {Id})", timeSlot.Label, timeSlot.Id);

        var existing = await GetTimeSlotByIdAsync(timeSlot.Id);
        if (existing != null)
        {
            // Update existing
            existing.Label = timeSlot.Label;
            existing.StartTime = timeSlot.StartTime;
            existing.EndTime = timeSlot.EndTime;
            existing.IsPeriod = timeSlot.IsPeriod;
            existing.LastUpdated = DateTime.UtcNow;
            _logger.LogDebug("Updated existing timeslot '{Label}'", timeSlot.Label);
        }
        else
        {
            // Add new
            _context.TimeSlots.Add(timeSlot);
            _logger.LogDebug("Added new timeslot '{Label}'", timeSlot.Label);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("TIMESLOT SAVED: '{Label}' (ID: {Id})", timeSlot.Label, timeSlot.Id);
        return existing ?? timeSlot;
    }

    public async Task<bool> DeleteTimeSlotAsync(string id)
    {
        _logger.LogInformation("DeleteTimeSlot called for ID: '{Id}'", id);

        var timeSlot = await GetTimeSlotByIdAsync(id);
        if (timeSlot == null)
        {
            _logger.LogWarning("Cannot delete timeslot '{Id}': Not found", id);
            return false;
        }

        _context.TimeSlots.Remove(timeSlot);
        await _context.SaveChangesAsync();
        _logger.LogInformation("TIMESLOT DELETED: '{Label}' (ID: {Id})", timeSlot.Label, id);
        return true;
    }

    public async Task ClearAllTimeSlotsAsync()
    {
        _logger.LogInformation("Clearing all timeslots from database");
        var allTimeSlots = await _context.TimeSlots.ToListAsync();
        _context.TimeSlots.RemoveRange(allTimeSlots);
        await _context.SaveChangesAsync();
        _logger.LogInformation("CLEARED {Count} timeslots", allTimeSlots.Count);
    }

    public async Task SaveAllTimeSlotsAsync(List<TimeSlot> timeSlots)
    {
        _logger.LogInformation("Saving {Count} timeslots to database", timeSlots.Count);

        // Clear existing and add all new ones
        await ClearAllTimeSlotsAsync();

        foreach (var timeSlot in timeSlots)
        {
            timeSlot.LastUpdated = DateTime.UtcNow;
            _context.TimeSlots.Add(timeSlot);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("SAVED {Count} timeslots to database", timeSlots.Count);
    }
}
