using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinterAdventurer.Data;
using WinterAdventurer.Services;

namespace WinterAdventurer.Test;

/// <summary>
/// Tests for TagService CRUD operations and tag-location relationships.
/// </summary>
[TestClass]
public class TagServiceTests
{
    private ApplicationDbContext _context = null!;
    private ILogger<TagService> _logger = null!;
    private TagService _tagService = null!;

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
            .CreateLogger<TagService>();

        _tagService = new TagService(_context, _logger);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetAllTagsAsync Tests

    [TestMethod]
    public async Task GetAllTagsAsync_NoTags_ReturnsEmptyList()
    {
        // Act
        var result = await _tagService.GetAllTagsAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetAllTagsAsync_HasTags_ReturnsSortedAlphabetically()
    {
        // Arrange
        _context.Tags.AddRange(
            new Tag { Name = "Zebra", Color = "#000000", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "Alpha", Color = "#111111", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "Mike", Color = "#222222", CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _tagService.GetAllTagsAsync();

        // Assert
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("Alpha", result[0].Name);
        Assert.AreEqual("Mike", result[1].Name);
        Assert.AreEqual("Zebra", result[2].Name);
    }

    #endregion

    #region GetTagByNameAsync Tests

    [TestMethod]
    public async Task GetTagByNameAsync_TagExists_ReturnsTag()
    {
        // Arrange
        var tag = new Tag { Name = "TestTag", Color = "#FF0000", CreatedAt = DateTime.UtcNow };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tagService.GetTagByNameAsync("TestTag");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("TestTag", result.Name);
        Assert.AreEqual("#FF0000", result.Color);
    }

    [TestMethod]
    public async Task GetTagByNameAsync_TagDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _tagService.GetTagByNameAsync("NonExistent");

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region CreateTagAsync Tests

    [TestMethod]
    public async Task CreateTagAsync_ValidName_CreatesTag()
    {
        // Act
        var result = await _tagService.CreateTagAsync("NewTag", "#00FF00");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("NewTag", result.Name);
        Assert.AreEqual("#00FF00", result.Color);
        Assert.IsTrue(result.Id > 0);

        // Verify it's in the database
        var dbTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == "NewTag");
        Assert.IsNotNull(dbTag);
    }

    [TestMethod]
    public async Task CreateTagAsync_NoColor_CreatesTagWithNullColor()
    {
        // Act
        var result = await _tagService.CreateTagAsync("TagWithoutColor");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("TagWithoutColor", result.Name);
        Assert.IsNull(result.Color);
    }

    [TestMethod]
    public async Task CreateTagAsync_NullName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            async () => await _tagService.CreateTagAsync(null!));
    }

    [TestMethod]
    public async Task CreateTagAsync_EmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            async () => await _tagService.CreateTagAsync(""));
    }

    [TestMethod]
    public async Task CreateTagAsync_WhitespaceName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            async () => await _tagService.CreateTagAsync("   "));
    }

    [TestMethod]
    public async Task CreateTagAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        await _tagService.CreateTagAsync("DuplicateTag");

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await _tagService.CreateTagAsync("DuplicateTag"));
    }

    #endregion

    #region DeleteTagAsync Tests

    [TestMethod]
    public async Task DeleteTagAsync_TagExists_DeletesAndReturnsTrue()
    {
        // Arrange
        var tag = new Tag { Name = "ToDelete", Color = "#FF0000", CreatedAt = DateTime.UtcNow };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tagService.DeleteTagAsync("ToDelete");

        // Assert
        Assert.IsTrue(result);
        var dbTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == "ToDelete");
        Assert.IsNull(dbTag);
    }

    [TestMethod]
    public async Task DeleteTagAsync_TagDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = await _tagService.DeleteTagAsync("NonExistent");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task DeleteTagAsync_TagAssignedToLocation_ThrowsInvalidOperationException()
    {
        // Arrange
        var tag = new Tag { Name = "AssignedTag", Color = "#FF0000", CreatedAt = DateTime.UtcNow };
        var location = new Location { Name = "TestLocation", CreatedAt = DateTime.UtcNow };
        location.Tags.Add(tag);
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await _tagService.DeleteTagAsync("AssignedTag"));
    }

    [TestMethod]
    public async Task DeleteTagAsync_TagAssignedToMultipleLocations_ExceptionMessageShowsCount()
    {
        // Arrange
        var tag = new Tag { Name = "PopularTag", Color = "#FF0000", CreatedAt = DateTime.UtcNow };
        var location1 = new Location { Name = "Location1", CreatedAt = DateTime.UtcNow };
        var location2 = new Location { Name = "Location2", CreatedAt = DateTime.UtcNow };
        location1.Tags.Add(tag);
        location2.Tags.Add(tag);
        _context.Locations.AddRange(location1, location2);
        await _context.SaveChangesAsync();

        // Act & Assert
        try
        {
            await _tagService.DeleteTagAsync("PopularTag");
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException ex)
        {
            Assert.IsTrue(ex.Message.Contains("2 location(s)"));
        }
    }

    #endregion

    #region GetTagsForLocationAsync Tests

    [TestMethod]
    public async Task GetTagsForLocationAsync_LocationExists_ReturnsTags()
    {
        // Arrange
        var location = new Location { Name = "TestLocation", CreatedAt = DateTime.UtcNow };
        var tag1 = new Tag { Name = "Zebra", Color = "#000000", CreatedAt = DateTime.UtcNow };
        var tag2 = new Tag { Name = "Alpha", Color = "#111111", CreatedAt = DateTime.UtcNow };
        location.Tags.Add(tag1);
        location.Tags.Add(tag2);
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tagService.GetTagsForLocationAsync("TestLocation");

        // Assert
        Assert.AreEqual(2, result.Count);
        // Should be sorted alphabetically
        Assert.AreEqual("Alpha", result[0].Name);
        Assert.AreEqual("Zebra", result[1].Name);
    }

    [TestMethod]
    public async Task GetTagsForLocationAsync_LocationDoesNotExist_ReturnsEmptyList()
    {
        // Act
        var result = await _tagService.GetTagsForLocationAsync("NonExistent");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetTagsForLocationAsync_LocationHasNoTags_ReturnsEmptyList()
    {
        // Arrange
        var location = new Location { Name = "EmptyLocation", CreatedAt = DateTime.UtcNow };
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tagService.GetTagsForLocationAsync("EmptyLocation");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    #endregion

    #region AssignTagToLocationAsync Tests

    [TestMethod]
    public async Task AssignTagToLocationAsync_ValidTagAndLocation_AssignsTag()
    {
        // Arrange
        var tag = new Tag { Name = "TestTag", Color = "#FF0000", CreatedAt = DateTime.UtcNow };
        var location = new Location { Name = "TestLocation", CreatedAt = DateTime.UtcNow };
        _context.Tags.Add(tag);
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        // Act
        await _tagService.AssignTagToLocationAsync("TestLocation", "TestTag");

        // Assert
        var updatedLocation = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == "TestLocation");
        Assert.IsNotNull(updatedLocation);
        Assert.AreEqual(1, updatedLocation.Tags.Count);
        Assert.AreEqual("TestTag", updatedLocation.Tags.First().Name);
    }

    [TestMethod]
    public async Task AssignTagToLocationAsync_LocationDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var tag = new Tag { Name = "TestTag", Color = "#FF0000", CreatedAt = DateTime.UtcNow };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await _tagService.AssignTagToLocationAsync("NonExistent", "TestTag"));
    }

    [TestMethod]
    public async Task AssignTagToLocationAsync_TagDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var location = new Location { Name = "TestLocation", CreatedAt = DateTime.UtcNow };
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await _tagService.AssignTagToLocationAsync("TestLocation", "NonExistent"));
    }

    [TestMethod]
    public async Task AssignTagToLocationAsync_TagAlreadyAssigned_IsIdempotent()
    {
        // Arrange
        var tag = new Tag { Name = "TestTag", Color = "#FF0000", CreatedAt = DateTime.UtcNow };
        var location = new Location { Name = "TestLocation", CreatedAt = DateTime.UtcNow };
        location.Tags.Add(tag);
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        // Act - assign again
        await _tagService.AssignTagToLocationAsync("TestLocation", "TestTag");

        // Assert - should still only have one tag
        var updatedLocation = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == "TestLocation");
        Assert.IsNotNull(updatedLocation);
        Assert.AreEqual(1, updatedLocation.Tags.Count);
    }

    #endregion

    #region RemoveTagFromLocationAsync Tests

    [TestMethod]
    public async Task RemoveTagFromLocationAsync_ValidAssignment_RemovesAndReturnsTrue()
    {
        // Arrange
        var tag = new Tag { Name = "TestTag", Color = "#FF0000", CreatedAt = DateTime.UtcNow };
        var location = new Location { Name = "TestLocation", CreatedAt = DateTime.UtcNow };
        location.Tags.Add(tag);
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tagService.RemoveTagFromLocationAsync("TestLocation", "TestTag");

        // Assert
        Assert.IsTrue(result);
        var updatedLocation = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == "TestLocation");
        Assert.IsNotNull(updatedLocation);
        Assert.AreEqual(0, updatedLocation.Tags.Count);
    }

    [TestMethod]
    public async Task RemoveTagFromLocationAsync_LocationDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = await _tagService.RemoveTagFromLocationAsync("NonExistent", "TestTag");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task RemoveTagFromLocationAsync_TagNotAssigned_ReturnsFalse()
    {
        // Arrange
        var location = new Location { Name = "TestLocation", CreatedAt = DateTime.UtcNow };
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tagService.RemoveTagFromLocationAsync("TestLocation", "NonExistent");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task RemoveTagFromLocationAsync_LocationHasOtherTags_OnlyRemovesSpecifiedTag()
    {
        // Arrange
        var tag1 = new Tag { Name = "Tag1", Color = "#FF0000", CreatedAt = DateTime.UtcNow };
        var tag2 = new Tag { Name = "Tag2", Color = "#00FF00", CreatedAt = DateTime.UtcNow };
        var location = new Location { Name = "TestLocation", CreatedAt = DateTime.UtcNow };
        location.Tags.Add(tag1);
        location.Tags.Add(tag2);
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tagService.RemoveTagFromLocationAsync("TestLocation", "Tag1");

        // Assert
        Assert.IsTrue(result);
        var updatedLocation = await _context.Locations
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Name == "TestLocation");
        Assert.IsNotNull(updatedLocation);
        Assert.AreEqual(1, updatedLocation.Tags.Count);
        Assert.AreEqual("Tag2", updatedLocation.Tags.First().Name);
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public async Task TagService_CompleteWorkflow_CreatesTagsAssignsToLocationAndDeletes()
    {
        // Create location
        var location = new Location { Name = "Workshop", CreatedAt = DateTime.UtcNow };
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        // Create tags
        var tag1 = await _tagService.CreateTagAsync("Downstairs", "#FF5722");
        var tag2 = await _tagService.CreateTagAsync("Accessible", "#4CAF50");

        // Assign tags to location
        await _tagService.AssignTagToLocationAsync("Workshop", "Downstairs");
        await _tagService.AssignTagToLocationAsync("Workshop", "Accessible");

        // Verify assignments
        var tags = await _tagService.GetTagsForLocationAsync("Workshop");
        Assert.AreEqual(2, tags.Count);

        // Remove one tag
        var removed = await _tagService.RemoveTagFromLocationAsync("Workshop", "Downstairs");
        Assert.IsTrue(removed);

        // Verify only one tag remains
        tags = await _tagService.GetTagsForLocationAsync("Workshop");
        Assert.AreEqual(1, tags.Count);
        Assert.AreEqual("Accessible", tags[0].Name);

        // Delete the unassigned tag
        var deleted = await _tagService.DeleteTagAsync("Downstairs");
        Assert.IsTrue(deleted);

        // Verify all tags
        var allTags = await _tagService.GetAllTagsAsync();
        Assert.AreEqual(1, allTags.Count);
        Assert.AreEqual("Accessible", allTags[0].Name);
    }

    #endregion
}
