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
public partial class LocationService : ILocationService
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
        LogLoadingAllLocationNames();
        var locations = await _context.Locations
            .OrderBy(l => l.Name)
            .Select(l => l.Name)
            .ToListAsync();
        LogLoadedLocations(locations.Count);
        return locations;
    }

    /// <inheritdoc />
    public async Task<List<Location>> GetAllLocationsWithTagsAsync()
    {
        LogLoadingAllLocationsWithTags();
        var locations = await _context.Locations
            .Include(l => l.Tags)
            .OrderBy(l => l.Name)
            .ToListAsync();
        LogLoadedLocationsWithTags(locations.Count);
        return locations;
    }

    /// <inheritdoc />
    public async Task<Location?> GetLocationByNameAsync(string name)
    {
        LogLookingUpLocationByName(name);
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Name == name);
        LogLocationLookupResult(location != null ? "FOUND" : "NOT FOUND");
        return location;
    }

    /// <inheritdoc />
    public async Task<Location> AddOrGetLocationAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            LogWarningAttemptedToAddEmptyLocationName();
            throw new ArgumentException("Location name cannot be empty", nameof(name));
        }

        LogInformationAddOrGetLocationCalled(name);

        var existing = await GetLocationByNameAsync(name);
        if (existing != null)
        {
            LogLocationAlreadyExists(name, existing.Id);
            return existing;
        }

        var location = new Location { Name = name };
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();
        LogNewLocationSaved(name, location.Id);
        return location;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteLocationAsync(string name)
    {
        LogDeleteLocationCalled(name);

        var location = await GetLocationByNameAsync(name);
        if (location == null)
        {
            LogWarningCannotDeleteLocationNotFound(name);
            return false;
        }

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync();
        LogLocationDeleted(name, location.Id);
        return true;
    }

    /// <inheritdoc />
    public async Task<string?> GetWorkshopLocationMappingAsync(string workshopName)
    {
        LogLookingUpWorkshopLocationMapping(workshopName);

        var mapping = await _context.WorkshopLocationMappings
            .FirstOrDefaultAsync(m => m.WorkshopName == workshopName);

        var result = mapping?.LocationName;
        LogWorkshopMappingResult(workshopName, result ?? "NOT FOUND");

        return result;
    }

    /// <inheritdoc />
    public async Task SaveWorkshopLocationMappingAsync(string workshopName, string locationName)
    {
        LogSavingWorkshopLocationMapping(workshopName, locationName);

        var existing = await _context.WorkshopLocationMappings
            .FirstOrDefaultAsync(m => m.WorkshopName == workshopName);

        if (existing != null)
        {
            existing.LocationName = locationName;
            existing.LastUpdated = DateTime.UtcNow;
            LogUpdatedExistingMapping(workshopName);
        }
        else
        {
            var mapping = new WorkshopLocationMapping
            {
                WorkshopName = workshopName,
                LocationName = locationName
            };
            _context.WorkshopLocationMappings.Add(mapping);
            LogCreatedNewMapping(workshopName);
        }

        await _context.SaveChangesAsync();
        LogMappingSaved(workshopName, locationName);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetAllWorkshopLocationMappingsAsync()
    {
        LogLoadingAllWorkshopLocationMappings();

        var mappings = await _context.WorkshopLocationMappings
            .ToDictionaryAsync(m => m.WorkshopName, m => m.LocationName);

        LogLoadedWorkshopLocationMappings(mappings.Count);
        return mappings;
    }

    /// <inheritdoc />
    public async Task<List<TimeSlot>> GetAllTimeSlotsAsync()
    {
        LogLoadingAllTimeSlots();
        var timeSlots = await _context.TimeSlots.ToListAsync();
        LogLoadedTimeSlots(timeSlots.Count);
        return timeSlots;
    }

    /// <inheritdoc />
    public async Task<TimeSlot?> GetTimeSlotByIdAsync(string id)
    {
        LogLookingUpTimeSlotById(id);
        var timeSlot = await _context.TimeSlots.FirstOrDefaultAsync(t => t.Id == id);
        LogTimeSlotLookupResult(timeSlot != null ? "FOUND" : "NOT FOUND");
        return timeSlot;
    }

    /// <inheritdoc />
    public async Task<TimeSlot> SaveTimeSlotAsync(TimeSlot timeSlot)
    {
        LogSavingTimeSlot(timeSlot.Label, timeSlot.Id);

        var existing = await GetTimeSlotByIdAsync(timeSlot.Id);
        if (existing != null)
        {
            // Update existing
            existing.Label = timeSlot.Label;
            existing.StartTime = timeSlot.StartTime;
            existing.EndTime = timeSlot.EndTime;
            existing.IsPeriod = timeSlot.IsPeriod;
            existing.LastUpdated = DateTime.UtcNow;
            LogUpdatedExistingTimeSlot(timeSlot.Label);
        }
        else
        {
            // Add new
            _context.TimeSlots.Add(timeSlot);
            LogAddedNewTimeSlot(timeSlot.Label);
        }

        await _context.SaveChangesAsync();
        LogTimeSlotSaved(timeSlot.Label, timeSlot.Id);
        return existing ?? timeSlot;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTimeSlotAsync(string id)
    {
        LogDeleteTimeSlotCalled(id);

        var timeSlot = await GetTimeSlotByIdAsync(id);
        if (timeSlot == null)
        {
            LogWarningCannotDeleteTimeSlotNotFound(id);
            return false;
        }

        _context.TimeSlots.Remove(timeSlot);
        await _context.SaveChangesAsync();
        LogTimeSlotDeleted(timeSlot.Label, id);
        return true;
    }

    /// <inheritdoc />
    public async Task ClearAllTimeSlotsAsync()
    {
        LogClearingAllTimeSlots();
        var allTimeSlots = await _context.TimeSlots.ToListAsync();
        _context.TimeSlots.RemoveRange(allTimeSlots);
        await _context.SaveChangesAsync();
        LogClearedTimeSlots(allTimeSlots.Count);
    }

    /// <inheritdoc />
    public async Task SaveAllTimeSlotsAsync(List<TimeSlot> timeSlots)
    {
        LogSavingAllTimeSlots(timeSlots.Count);

        // Clear existing and add all new ones
        await ClearAllTimeSlotsAsync();

        foreach (var timeSlot in timeSlots)
        {
            timeSlot.LastUpdated = DateTime.UtcNow;
            _context.TimeSlots.Add(timeSlot);
        }

        await _context.SaveChangesAsync();
        LogSavedAllTimeSlots(timeSlots.Count);
    }

    #region Logging

    // Location Operations: 7001-7099
    [LoggerMessage(EventId = 7001, Level = LogLevel.Debug, Message = "Loading all location names from database")]
    partial void LogLoadingAllLocationNames();

    [LoggerMessage(EventId = 7002, Level = LogLevel.Debug, Message = "Loaded {Count} locations")]
    partial void LogLoadedLocations(int count);

    [LoggerMessage(EventId = 7003, Level = LogLevel.Debug, Message = "Loading all locations with tags from database")]
    partial void LogLoadingAllLocationsWithTags();

    [LoggerMessage(EventId = 7004, Level = LogLevel.Debug, Message = "Loaded {Count} locations with tags")]
    partial void LogLoadedLocationsWithTags(int count);

    [LoggerMessage(EventId = 7005, Level = LogLevel.Debug, Message = "Looking up location by name: '{Name}'")]
    partial void LogLookingUpLocationByName(string name);

    [LoggerMessage(EventId = 7006, Level = LogLevel.Debug, Message = "Location lookup result: {Found}")]
    partial void LogLocationLookupResult(string found);

    [LoggerMessage(EventId = 7007, Level = LogLevel.Warning, Message = "Attempted to add empty/null location name")]
    partial void LogWarningAttemptedToAddEmptyLocationName();

    [LoggerMessage(EventId = 7008, Level = LogLevel.Information, Message = "AddOrGetLocation called for: '{Name}'")]
    partial void LogInformationAddOrGetLocationCalled(string name);

    [LoggerMessage(EventId = 7009, Level = LogLevel.Debug, Message = "Location '{Name}' already exists with ID {Id}")]
    partial void LogLocationAlreadyExists(string name, int id);

    [LoggerMessage(EventId = 7010, Level = LogLevel.Information, Message = "NEW LOCATION SAVED: '{Name}' (ID: {Id})")]
    partial void LogNewLocationSaved(string name, int id);

    [LoggerMessage(EventId = 7011, Level = LogLevel.Information, Message = "DeleteLocation called for: '{Name}'")]
    partial void LogDeleteLocationCalled(string name);

    [LoggerMessage(EventId = 7012, Level = LogLevel.Warning, Message = "Cannot delete location '{Name}': Not found")]
    partial void LogWarningCannotDeleteLocationNotFound(string name);

    [LoggerMessage(EventId = 7013, Level = LogLevel.Information, Message = "LOCATION DELETED: '{Name}' (ID: {Id})")]
    partial void LogLocationDeleted(string name, int id);

    // Workshop-Location Mapping Operations: 7100-7199
    [LoggerMessage(EventId = 7100, Level = LogLevel.Debug, Message = "Looking up location mapping for workshop: '{WorkshopName}'")]
    partial void LogLookingUpWorkshopLocationMapping(string workshopName);

    [LoggerMessage(EventId = 7101, Level = LogLevel.Debug, Message = "Workshop '{WorkshopName}' mapping result: {Location}")]
    partial void LogWorkshopMappingResult(string workshopName, string location);

    [LoggerMessage(EventId = 7102, Level = LogLevel.Information, Message = "Saving workshop-location mapping: '{WorkshopName}' -> '{LocationName}'")]
    partial void LogSavingWorkshopLocationMapping(string workshopName, string locationName);

    [LoggerMessage(EventId = 7103, Level = LogLevel.Debug, Message = "Updated existing mapping for '{WorkshopName}'")]
    partial void LogUpdatedExistingMapping(string workshopName);

    [LoggerMessage(EventId = 7104, Level = LogLevel.Debug, Message = "Created new mapping for '{WorkshopName}'")]
    partial void LogCreatedNewMapping(string workshopName);

    [LoggerMessage(EventId = 7105, Level = LogLevel.Information, Message = "MAPPING SAVED: '{WorkshopName}' -> '{LocationName}'")]
    partial void LogMappingSaved(string workshopName, string locationName);

    [LoggerMessage(EventId = 7106, Level = LogLevel.Debug, Message = "Loading all workshop-location mappings from database")]
    partial void LogLoadingAllWorkshopLocationMappings();

    [LoggerMessage(EventId = 7107, Level = LogLevel.Debug, Message = "Loaded {Count} workshop-location mappings")]
    partial void LogLoadedWorkshopLocationMappings(int count);

    // TimeSlot Operations: 7200-7299
    [LoggerMessage(EventId = 7200, Level = LogLevel.Debug, Message = "Loading all timeslots from database")]
    partial void LogLoadingAllTimeSlots();

    [LoggerMessage(EventId = 7201, Level = LogLevel.Debug, Message = "Loaded {Count} timeslots")]
    partial void LogLoadedTimeSlots(int count);

    [LoggerMessage(EventId = 7202, Level = LogLevel.Debug, Message = "Looking up timeslot by ID: '{Id}'")]
    partial void LogLookingUpTimeSlotById(string id);

    [LoggerMessage(EventId = 7203, Level = LogLevel.Debug, Message = "TimeSlot lookup result: {Found}")]
    partial void LogTimeSlotLookupResult(string found);

    [LoggerMessage(EventId = 7204, Level = LogLevel.Information, Message = "Saving timeslot: '{Label}' (ID: {Id})")]
    partial void LogSavingTimeSlot(string label, string id);

    [LoggerMessage(EventId = 7205, Level = LogLevel.Debug, Message = "Updated existing timeslot '{Label}'")]
    partial void LogUpdatedExistingTimeSlot(string label);

    [LoggerMessage(EventId = 7206, Level = LogLevel.Debug, Message = "Added new timeslot '{Label}'")]
    partial void LogAddedNewTimeSlot(string label);

    [LoggerMessage(EventId = 7207, Level = LogLevel.Information, Message = "TIMESLOT SAVED: '{Label}' (ID: {Id})")]
    partial void LogTimeSlotSaved(string label, string id);

    [LoggerMessage(EventId = 7208, Level = LogLevel.Information, Message = "DeleteTimeSlot called for ID: '{Id}'")]
    partial void LogDeleteTimeSlotCalled(string id);

    [LoggerMessage(EventId = 7209, Level = LogLevel.Warning, Message = "Cannot delete timeslot '{Id}': Not found")]
    partial void LogWarningCannotDeleteTimeSlotNotFound(string id);

    [LoggerMessage(EventId = 7210, Level = LogLevel.Information, Message = "TIMESLOT DELETED: '{Label}' (ID: {Id})")]
    partial void LogTimeSlotDeleted(string label, string id);

    [LoggerMessage(EventId = 7211, Level = LogLevel.Information, Message = "Clearing all timeslots from database")]
    partial void LogClearingAllTimeSlots();

    [LoggerMessage(EventId = 7212, Level = LogLevel.Information, Message = "CLEARED {Count} timeslots")]
    partial void LogClearedTimeSlots(int count);

    [LoggerMessage(EventId = 7213, Level = LogLevel.Information, Message = "Saving {Count} timeslots to database")]
    partial void LogSavingAllTimeSlots(int count);

    [LoggerMessage(EventId = 7214, Level = LogLevel.Information, Message = "SAVED {Count} timeslots to database")]
    partial void LogSavedAllTimeSlots(int count);

    #endregion
}
