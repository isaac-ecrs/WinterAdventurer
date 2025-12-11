using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinterAdventurer.Data;

namespace WinterAdventurer.Test;

/// <summary>
/// Tests for DbSeeder default data seeding functionality.
/// </summary>
[TestClass]
public class DbSeederTests
{
    private ApplicationDbContext _context = null!;
    private ILogger<DbSeeder> _logger = null!;
    private DbSeeder _dbSeeder = null!;

    [TestInitialize]
    public void Setup()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Create logger
        _logger = LoggerFactory.Create(builder => builder.AddConsole())
                    .CreateLogger<DbSeeder>();

        // Create DB context
        _dbSeeder = new DbSeeder(_logger);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region SeedDefaultDataAsync Tests

    [TestMethod]
    public async Task SeedDefaultDataAsync_EmptyDatabase_SeedsDefaultLocationsAndTags()
    {
        // Act
        await _dbSeeder.SeedDefaultDataAsync(_context);

        // Assert - verify tags were created
        var tags = await _context.Tags.ToListAsync();
        Assert.IsTrue(tags.Count > 0, "Should have seeded at least one tag");

        // Assert - verify locations were created
        var locations = await _context.Locations.ToListAsync();
        Assert.IsTrue(locations.Count > 0, "Should have seeded at least one location");
    }

    [TestMethod]
    public async Task SeedDefaultDataAsync_EmptyDatabase_CreatesDownstairsTag()
    {
        // Act
        await _dbSeeder.SeedDefaultDataAsync(_context);

        // Assert
        var downstairsTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == "Downstairs");
        Assert.IsNotNull(downstairsTag, "Should have created 'Downstairs' tag");
        Assert.AreEqual("#FF5722", downstairsTag.Color);
    }

    [TestMethod]
    public async Task SeedDefaultDataAsync_EmptyDatabase_CreatesExpectedLocations()
    {
        // Arrange - expected default locations
        var expectedLocations = new[]
        {
            "Dining Room",
            "Martin Room",
            "Chapel A",
            "Craft Room",
            "Elm Room",
            "Rec Hall",
            "Library"
        };

        // Act
        await _dbSeeder.SeedDefaultDataAsync(_context);

        // Assert
        foreach (var expectedLocation in expectedLocations)
        {
            var location = await _context.Locations.FirstOrDefaultAsync(l => l.Name == expectedLocation);
            Assert.IsNotNull(location, $"Should have created '{expectedLocation}' location");
        }
    }

    [TestMethod]
    public async Task SeedDefaultDataAsync_EmptyDatabase_AssignsDownstairsTagToCorrectLocations()
    {
        // Act
        await _dbSeeder.SeedDefaultDataAsync(_context);

        // Assert - Craft Room should have Downstairs tag
        var craftRoom = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == "Craft Room");
        Assert.IsNotNull(craftRoom);
        Assert.AreEqual(1, craftRoom.Tags.Count);
        Assert.AreEqual("Downstairs", craftRoom.Tags.First().Name);

        // Assert - Rec Hall should have Downstairs tag
        var recHall = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == "Rec Hall");
        Assert.IsNotNull(recHall);
        Assert.AreEqual(1, recHall.Tags.Count);
        Assert.AreEqual("Downstairs", recHall.Tags.First().Name);
    }

    [TestMethod]
    public async Task SeedDefaultDataAsync_EmptyDatabase_LocationsWithoutTagsHaveEmptyTagsList()
    {
        // Act
        await _dbSeeder.SeedDefaultDataAsync(_context);

        // Assert - Dining Room should have no tags
        var diningRoom = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == "Dining Room");
        Assert.IsNotNull(diningRoom);
        Assert.AreEqual(0, diningRoom.Tags.Count);

        // Assert - Martin Room should have no tags
        var martinRoom = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == "Martin Room");
        Assert.IsNotNull(martinRoom);
        Assert.AreEqual(0, martinRoom.Tags.Count);
    }

    [TestMethod]
    public async Task SeedDefaultDataAsync_AlreadySeeded_IsIdempotent()
    {
        // Arrange - seed once
        await _dbSeeder.SeedDefaultDataAsync(_context);
        var initialTagCount = await _context.Tags.CountAsync();
        var initialLocationCount = await _context.Locations.CountAsync();

        // Act - seed again
        await _dbSeeder.SeedDefaultDataAsync(_context);

        // Assert - counts should not change
        var finalTagCount = await _context.Tags.CountAsync();
        var finalLocationCount = await _context.Locations.CountAsync();
        Assert.AreEqual(initialTagCount, finalTagCount, "Tag count should not change");
        Assert.AreEqual(initialLocationCount, finalLocationCount, "Location count should not change");
    }

    [TestMethod]
    public async Task SeedDefaultDataAsync_PartiallySeeded_OnlyAddsNewData()
    {
        // Arrange - manually add one of the default tags
        _context.Tags.Add(new Tag
        {
            Name = "Downstairs",
            Color = "#FF5722",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act - run seeder
        await _dbSeeder.SeedDefaultDataAsync(_context);

        // Assert - should still only have one tag (not duplicated)
        var tags = await _context.Tags.Where(t => t.Name == "Downstairs").ToListAsync();
        Assert.AreEqual(1, tags.Count, "Should not duplicate existing tag");
    }

    [TestMethod]
    public async Task SeedDefaultDataAsync_PartiallySeededLocations_OnlyAddsNewLocations()
    {
        // Arrange - manually add one of the default locations
        _context.Locations.Add(new Location
        {
            Name = "Dining Room",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act - run seeder
        await _dbSeeder.SeedDefaultDataAsync(_context);

        // Assert - should still only have one Dining Room (not duplicated)
        var diningRooms = await _context.Locations.Where(l => l.Name == "Dining Room").ToListAsync();
        Assert.AreEqual(1, diningRooms.Count, "Should not duplicate existing location");
    }

    [TestMethod]
    public async Task SeedDefaultDataAsync_TagAlreadyAssigned_DoesNotDuplicateAssignment()
    {
        // Arrange - manually create tag, location, and assignment
        var tag = new Tag { Name = "Downstairs", Color = "#FF5722", CreatedAt = DateTime.UtcNow };
        var location = new Location { Name = "Craft Room", CreatedAt = DateTime.UtcNow };
        location.Tags.Add(tag);
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        // Clear tracking
        _context.ChangeTracker.Clear();

        // Act - run seeder
        await _dbSeeder.SeedDefaultDataAsync(_context);

        // Assert - should still only have one tag assignment
        var craftRoom = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == "Craft Room");
        Assert.IsNotNull(craftRoom);
        Assert.AreEqual(1, craftRoom.Tags.Count, "Should not duplicate tag assignment");
    }

    [TestMethod]
    public async Task SeedDefaultDataAsync_CreatesTimestamps()
    {
        // Act
        await _dbSeeder.SeedDefaultDataAsync(_context);

        // Assert - verify tags have CreatedAt timestamps
        var tag = await _context.Tags.FirstAsync();
        Assert.IsTrue(tag.CreatedAt > DateTime.MinValue);
        Assert.IsTrue(tag.CreatedAt <= DateTime.UtcNow);

        // Assert - verify locations have CreatedAt timestamps
        var location = await _context.Locations.FirstAsync();
        Assert.IsTrue(location.CreatedAt > DateTime.MinValue);
        Assert.IsTrue(location.CreatedAt <= DateTime.UtcNow);
    }

    [TestMethod]
    public async Task SeedDefaultDataAsync_MultipleRuns_RemainsIdempotent()
    {
        // Act - seed multiple times
        await _dbSeeder.SeedDefaultDataAsync(_context);
        var count1 = await _context.Locations.CountAsync();

        await _dbSeeder.SeedDefaultDataAsync(_context);
        var count2 = await _context.Locations.CountAsync();

        await _dbSeeder.SeedDefaultDataAsync(_context);
        var count3 = await _context.Locations.CountAsync();

        // Assert - all counts should be the same
        Assert.AreEqual(count1, count2);
        Assert.AreEqual(count2, count3);
    }

    [TestMethod]
    public async Task SeedDefaultDataAsync_EmptyDatabase_TagAssignmentsMatchExpectedConfiguration()
    {
        // Act
        await _dbSeeder.SeedDefaultDataAsync(_context);

        // Assert - only Craft Room and Rec Hall should have tags
        var locationsWithTags = await _context.Locations
            .Include(l => l.Tags)
            .Where(l => l.Tags.Any())
            .ToListAsync();

        Assert.AreEqual(2, locationsWithTags.Count, "Only 2 locations should have tags");

        var locationNames = locationsWithTags.Select(l => l.Name).OrderBy(n => n).ToList();
        Assert.AreEqual("Craft Room", locationNames[0]);
        Assert.AreEqual("Rec Hall", locationNames[1]);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public async Task SeedDefaultDataAsync_CustomLocationExists_DoesNotConflict()
    {
        // Arrange - add a custom location that's not in the default set
        _context.Locations.Add(new Location
        {
            Name = "Custom Workshop",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _dbSeeder.SeedDefaultDataAsync(_context);

        // Assert - custom location should still exist
        var customLocation = await _context.Locations.FirstOrDefaultAsync(l => l.Name == "Custom Workshop");
        Assert.IsNotNull(customLocation);

        // Assert - all default locations should also exist
        var totalLocations = await _context.Locations.CountAsync();
        Assert.IsTrue(totalLocations >= 8, "Should have at least 7 default + 1 custom location");
    }

    [TestMethod]
    public async Task SeedDefaultDataAsync_CustomTagExists_DoesNotConflict()
    {
        // Arrange - add a custom tag that's not in the default set
        _context.Tags.Add(new Tag
        {
            Name = "Accessible",
            Color = "#00FF00",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _dbSeeder.SeedDefaultDataAsync(_context);

        // Assert - custom tag should still exist
        var customTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == "Accessible");
        Assert.IsNotNull(customTag);

        // Assert - all default tags should also exist
        var totalTags = await _context.Tags.CountAsync();
        Assert.IsTrue(totalTags >= 2, "Should have at least 1 default + 1 custom tag");
    }

    #endregion
}
