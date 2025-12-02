using Microsoft.EntityFrameworkCore;
using WinterAdventurer.Data;
using Microsoft.Extensions.Logging;

namespace WinterAdventurer.Services;

/// <summary>
/// Provides location management services for workshop assignments and event scheduling.
/// Manages locations, workshop-to-location mappings, and time slot configurations.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Retrieves all location names from the database in alphabetical order.
    /// </summary>
    /// <returns>A list of location names sorted alphabetically.</returns>
    Task<List<string>> GetAllLocationNamesAsync();

    /// <summary>
    /// Retrieves all locations with their associated tags using eager loading.
    /// </summary>
    /// <returns>A list of locations including their tag collections, sorted alphabetically by name.</returns>
    Task<List<Location>> GetAllLocationsWithTagsAsync();

    /// <summary>
    /// Retrieves a specific location by its name.
    /// </summary>
    /// <param name="name">The name of the location to retrieve.</param>
    /// <returns>The location entity if found; otherwise, null.</returns>
    Task<Location?> GetLocationByNameAsync(string name);

    /// <summary>
    /// Adds a new location to the database or retrieves it if it already exists.
    /// </summary>
    /// <param name="name">The name of the location to add or retrieve.</param>
    /// <returns>The existing or newly created location entity.</returns>
    /// <exception cref="ArgumentException">Thrown when the location name is null or whitespace.</exception>
    Task<Location> AddOrGetLocationAsync(string name);

    /// <summary>
    /// Deletes a location from the database by name.
    /// </summary>
    /// <param name="name">The name of the location to delete.</param>
    /// <returns>True if the location was deleted; false if it was not found.</returns>
    Task<bool> DeleteLocationAsync(string name);

    /// <summary>
    /// Retrieves the location name assigned to a specific workshop.
    /// </summary>
    /// <param name="workshopName">The name of the workshop to look up.</param>
    /// <returns>The assigned location name if found; otherwise, null.</returns>
    Task<string?> GetWorkshopLocationMappingAsync(string workshopName);

    /// <summary>
    /// Saves or updates the location assignment for a workshop.
    /// Creates a new mapping if one doesn't exist, or updates the existing mapping.
    /// </summary>
    /// <param name="workshopName">The name of the workshop.</param>
    /// <param name="locationName">The name of the location to assign.</param>
    Task SaveWorkshopLocationMappingAsync(string workshopName, string locationName);

    /// <summary>
    /// Retrieves all workshop-to-location mappings from the database.
    /// </summary>
    /// <returns>A dictionary mapping workshop names to their assigned location names.</returns>
    Task<Dictionary<string, string>> GetAllWorkshopLocationMappingsAsync();

    /// <summary>
    /// Retrieves all time slots from the database.
    /// </summary>
    /// <returns>A list of all time slot entities.</returns>
    Task<List<TimeSlot>> GetAllTimeSlotsAsync();

    /// <summary>
    /// Retrieves a specific time slot by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the time slot.</param>
    /// <returns>The time slot entity if found; otherwise, null.</returns>
    Task<TimeSlot?> GetTimeSlotByIdAsync(string id);

    /// <summary>
    /// Saves a time slot to the database, creating a new entry or updating an existing one.
    /// </summary>
    /// <param name="timeSlot">The time slot entity to save.</param>
    /// <returns>The saved time slot entity.</returns>
    Task<TimeSlot> SaveTimeSlotAsync(TimeSlot timeSlot);

    /// <summary>
    /// Deletes a time slot from the database by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the time slot to delete.</param>
    /// <returns>True if the time slot was deleted; false if it was not found.</returns>
    Task<bool> DeleteTimeSlotAsync(string id);

    /// <summary>
    /// Removes all time slots from the database.
    /// </summary>
    Task ClearAllTimeSlotsAsync();

    /// <summary>
    /// Clears all existing time slots and saves a new collection to the database.
    /// This operation replaces all existing time slots with the provided collection.
    /// </summary>
    /// <param name="timeSlots">The collection of time slots to save.</param>
    Task SaveAllTimeSlotsAsync(List<TimeSlot> timeSlots);
}

/// <summary>
/// Implementation of location management services using Entity Framework Core.
/// Provides CRUD operations for locations, workshop-location mappings, and time slots,
/// with comprehensive logging for debugging and audit purposes.
/// </summary>
public class LocationService : ILocationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LocationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationService"/> class.
    /// </summary>
    /// <param name="context">The Entity Framework database context for data access.</param>
    /// <param name="logger">The logger instance for diagnostic logging.</param>
    public LocationService(ApplicationDbContext context, ILogger<LocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<Location?> GetLocationByNameAsync(string name)
    {
        _logger.LogDebug("Looking up location by name: '{Name}'", name);
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Name == name);
        _logger.LogDebug("Location lookup result: {Found}", location != null ? "FOUND" : "NOT FOUND");
        return location;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetAllWorkshopLocationMappingsAsync()
    {
        _logger.LogDebug("Loading all workshop-location mappings from database");

        var mappings = await _context.WorkshopLocationMappings
            .ToDictionaryAsync(m => m.WorkshopName, m => m.LocationName);

        _logger.LogDebug("Loaded {Count} workshop-location mappings", mappings.Count);
        return mappings;
    }

    /// <inheritdoc />
    public async Task<List<TimeSlot>> GetAllTimeSlotsAsync()
    {
        _logger.LogDebug("Loading all timeslots from database");
        var timeSlots = await _context.TimeSlots.ToListAsync();
        _logger.LogDebug("Loaded {Count} timeslots", timeSlots.Count);
        return timeSlots;
    }

    /// <inheritdoc />
    public async Task<TimeSlot?> GetTimeSlotByIdAsync(string id)
    {
        _logger.LogDebug("Looking up timeslot by ID: '{Id}'", id);
        var timeSlot = await _context.TimeSlots.FirstOrDefaultAsync(t => t.Id == id);
        _logger.LogDebug("TimeSlot lookup result: {Found}", timeSlot != null ? "FOUND" : "NOT FOUND");
        return timeSlot;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task ClearAllTimeSlotsAsync()
    {
        _logger.LogInformation("Clearing all timeslots from database");
        var allTimeSlots = await _context.TimeSlots.ToListAsync();
        _context.TimeSlots.RemoveRange(allTimeSlots);
        await _context.SaveChangesAsync();
        _logger.LogInformation("CLEARED {Count} timeslots", allTimeSlots.Count);
    }

    /// <inheritdoc />
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
