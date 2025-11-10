using Microsoft.EntityFrameworkCore;
using WinterAdventurer.Data;

namespace WinterAdventurer.Services;

public interface ILocationService
{
    Task<List<string>> GetAllLocationNamesAsync();
    Task<Location?> GetLocationByNameAsync(string name);
    Task<Location> AddOrGetLocationAsync(string name);
}

public class LocationService : ILocationService
{
    private readonly ApplicationDbContext _context;

    public LocationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<string>> GetAllLocationNamesAsync()
    {
        return await _context.Locations
            .OrderBy(l => l.Name)
            .Select(l => l.Name)
            .ToListAsync();
    }

    public async Task<Location?> GetLocationByNameAsync(string name)
    {
        return await _context.Locations
            .FirstOrDefaultAsync(l => l.Name == name);
    }

    public async Task<Location> AddOrGetLocationAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Location name cannot be empty", nameof(name));
        }

        var existing = await GetLocationByNameAsync(name);
        if (existing != null)
        {
            return existing;
        }

        var location = new Location { Name = name };
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();
        return location;
    }
}
