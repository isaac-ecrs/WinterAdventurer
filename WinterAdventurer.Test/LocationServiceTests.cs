using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WinterAdventurer.Data;
using WinterAdventurer.Services;

namespace WinterAdventurer.Test
{
    [TestClass]
    public class LocationServiceTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private ILogger<LocationService> CreateLogger()
        {
            return new LoggerFactory().CreateLogger<LocationService>();
        }

        #region Location Tests

        [TestMethod]
        public async Task GetAllLocationNamesAsync_WhenEmpty_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.GetAllLocationNamesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task GetAllLocationNamesAsync_ReturnsAllLocationsOrderedByName()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Locations.AddRange(
                new Location { Name = "Chapel" },
                new Location { Name = "Dining Hall" },
                new Location { Name = "Auditorium" }
            );
            await context.SaveChangesAsync();

            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.GetAllLocationNamesAsync();

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("Auditorium", result[0]);
            Assert.AreEqual("Chapel", result[1]);
            Assert.AreEqual("Dining Hall", result[2]);
        }

        [TestMethod]
        public async Task GetLocationByNameAsync_WhenExists_ReturnsLocation()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Locations.Add(new Location { Name = "Chapel" });
            await context.SaveChangesAsync();

            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.GetLocationByNameAsync("Chapel");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Chapel", result.Name);
        }

        [TestMethod]
        public async Task GetLocationByNameAsync_WhenNotExists_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.GetLocationByNameAsync("NonExistent");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task AddOrGetLocationAsync_WhenNew_AddsLocation()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.AddOrGetLocationAsync("Chapel");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Chapel", result.Name);
            Assert.AreNotEqual(0, result.Id);

            // Verify it's in database
            var dbLocation = await context.Locations.FirstOrDefaultAsync(l => l.Name == "Chapel");
            Assert.IsNotNull(dbLocation);
        }

        [TestMethod]
        public async Task AddOrGetLocationAsync_WhenExists_ReturnsExisting()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var existingLocation = new Location { Name = "Chapel" };
            context.Locations.Add(existingLocation);
            await context.SaveChangesAsync();

            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.AddOrGetLocationAsync("Chapel");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(existingLocation.Id, result.Id);
            Assert.AreEqual("Chapel", result.Name);

            // Verify only one location in database
            var count = await context.Locations.CountAsync();
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task AddOrGetLocationAsync_WhenEmpty_ThrowsArgumentException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act & Assert
            await Assert.ThrowsExactlyAsync<ArgumentException>(
                async () => await service.AddOrGetLocationAsync("")
            );
        }

        [TestMethod]
        public async Task AddOrGetLocationAsync_WhenNull_ThrowsArgumentException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act & Assert
            await Assert.ThrowsExactlyAsync<ArgumentException>(
                async () => await service.AddOrGetLocationAsync(null!)
            );
        }

        [TestMethod]
        public async Task DeleteLocationAsync_WhenExists_DeletesAndReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Locations.Add(new Location { Name = "Chapel" });
            await context.SaveChangesAsync();

            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.DeleteLocationAsync("Chapel");

            // Assert
            Assert.IsTrue(result);

            // Verify it's deleted from database
            var dbLocation = await context.Locations.FirstOrDefaultAsync(l => l.Name == "Chapel");
            Assert.IsNull(dbLocation);
        }

        [TestMethod]
        public async Task DeleteLocationAsync_WhenNotExists_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.DeleteLocationAsync("NonExistent");

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region WorkshopLocationMapping Tests

        [TestMethod]
        public async Task GetWorkshopLocationMappingAsync_WhenExists_ReturnsLocationName()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.WorkshopLocationMappings.Add(new WorkshopLocationMapping
            {
                WorkshopName = "Pottery",
                LocationName = "Art Studio"
            });
            await context.SaveChangesAsync();

            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.GetWorkshopLocationMappingAsync("Pottery");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Art Studio", result);
        }

        [TestMethod]
        public async Task GetWorkshopLocationMappingAsync_WhenNotExists_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.GetWorkshopLocationMappingAsync("NonExistent");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SaveWorkshopLocationMappingAsync_WhenNew_CreatesMapping()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            await service.SaveWorkshopLocationMappingAsync("Pottery", "Art Studio");

            // Assert
            var mapping = await context.WorkshopLocationMappings
                .FirstOrDefaultAsync(m => m.WorkshopName == "Pottery");
            Assert.IsNotNull(mapping);
            Assert.AreEqual("Art Studio", mapping.LocationName);
        }

        [TestMethod]
        public async Task SaveWorkshopLocationMappingAsync_WhenExists_UpdatesMapping()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var existingMapping = new WorkshopLocationMapping
            {
                WorkshopName = "Pottery",
                LocationName = "Old Location",
                LastUpdated = DateTime.UtcNow.AddDays(-1)
            };
            context.WorkshopLocationMappings.Add(existingMapping);
            await context.SaveChangesAsync();

            var oldLastUpdated = existingMapping.LastUpdated;
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            await service.SaveWorkshopLocationMappingAsync("Pottery", "Art Studio");

            // Assert
            var mapping = await context.WorkshopLocationMappings
                .FirstOrDefaultAsync(m => m.WorkshopName == "Pottery");
            Assert.IsNotNull(mapping);
            Assert.AreEqual("Art Studio", mapping.LocationName);
            Assert.IsTrue(mapping.LastUpdated > oldLastUpdated);

            // Verify only one mapping exists
            var count = await context.WorkshopLocationMappings.CountAsync();
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task GetAllWorkshopLocationMappingsAsync_ReturnsAllMappings()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.WorkshopLocationMappings.AddRange(
                new WorkshopLocationMapping { WorkshopName = "Pottery", LocationName = "Art Studio" },
                new WorkshopLocationMapping { WorkshopName = "Woodworking", LocationName = "Workshop" },
                new WorkshopLocationMapping { WorkshopName = "Cooking", LocationName = "Kitchen" }
            );
            await context.SaveChangesAsync();

            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.GetAllWorkshopLocationMappingsAsync();

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("Art Studio", result["Pottery"]);
            Assert.AreEqual("Workshop", result["Woodworking"]);
            Assert.AreEqual("Kitchen", result["Cooking"]);
        }

        [TestMethod]
        public async Task GetAllWorkshopLocationMappingsAsync_WhenEmpty_ReturnsEmptyDictionary()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.GetAllWorkshopLocationMappingsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        #endregion

        #region TimeSlot Tests

        [TestMethod]
        public async Task GetAllTimeSlotsAsync_WhenEmpty_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.GetAllTimeSlotsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task GetAllTimeSlotsAsync_ReturnsAllTimeSlots()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.TimeSlots.AddRange(
                new TimeSlot { Id = "1", Label = "Morning", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(12, 0, 0), IsPeriod = true },
                new TimeSlot { Id = "2", Label = "Lunch", StartTime = new TimeSpan(12, 0, 0), EndTime = new TimeSpan(13, 0, 0), IsPeriod = false },
                new TimeSlot { Id = "3", Label = "Afternoon", StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsPeriod = true }
            );
            await context.SaveChangesAsync();

            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.GetAllTimeSlotsAsync();

            // Assert
            Assert.AreEqual(3, result.Count);
        }

        [TestMethod]
        public async Task GetTimeSlotByIdAsync_WhenExists_ReturnsTimeSlot()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.TimeSlots.Add(new TimeSlot
            {
                Id = "test-id",
                Label = "Morning",
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(12, 0, 0)
            });
            await context.SaveChangesAsync();

            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.GetTimeSlotByIdAsync("test-id");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test-id", result.Id);
            Assert.AreEqual("Morning", result.Label);
        }

        [TestMethod]
        public async Task GetTimeSlotByIdAsync_WhenNotExists_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.GetTimeSlotByIdAsync("nonexistent");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SaveTimeSlotAsync_WhenNew_AddsTimeSlot()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            var timeSlot = new TimeSlot
            {
                Id = "new-slot",
                Label = "Morning",
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                IsPeriod = true
            };

            // Act
            var result = await service.SaveTimeSlotAsync(timeSlot);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("new-slot", result.Id);

            // Verify it's in database
            var dbTimeSlot = await context.TimeSlots.FirstOrDefaultAsync(t => t.Id == "new-slot");
            Assert.IsNotNull(dbTimeSlot);
            Assert.AreEqual("Morning", dbTimeSlot.Label);
        }

        [TestMethod]
        public async Task SaveTimeSlotAsync_WhenExists_UpdatesTimeSlot()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var existingSlot = new TimeSlot
            {
                Id = "existing-slot",
                Label = "Old Label",
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                IsPeriod = false
            };
            context.TimeSlots.Add(existingSlot);
            await context.SaveChangesAsync();

            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            var updatedSlot = new TimeSlot
            {
                Id = "existing-slot",
                Label = "Updated Label",
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(14, 0, 0),
                IsPeriod = true
            };

            // Act
            var result = await service.SaveTimeSlotAsync(updatedSlot);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated Label", result.Label);
            Assert.AreEqual(new TimeSpan(10, 0, 0), result.StartTime);
            Assert.AreEqual(new TimeSpan(14, 0, 0), result.EndTime);
            Assert.IsTrue(result.IsPeriod);

            // Verify only one timeslot exists
            var count = await context.TimeSlots.CountAsync();
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task DeleteTimeSlotAsync_WhenExists_DeletesAndReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.TimeSlots.Add(new TimeSlot
            {
                Id = "test-slot",
                Label = "Morning"
            });
            await context.SaveChangesAsync();

            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.DeleteTimeSlotAsync("test-slot");

            // Assert
            Assert.IsTrue(result);

            // Verify it's deleted from database
            var dbTimeSlot = await context.TimeSlots.FirstOrDefaultAsync(t => t.Id == "test-slot");
            Assert.IsNull(dbTimeSlot);
        }

        [TestMethod]
        public async Task DeleteTimeSlotAsync_WhenNotExists_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            var result = await service.DeleteTimeSlotAsync("nonexistent");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ClearAllTimeSlotsAsync_RemovesAllTimeSlots()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.TimeSlots.AddRange(
                new TimeSlot { Id = "1", Label = "Morning" },
                new TimeSlot { Id = "2", Label = "Lunch" },
                new TimeSlot { Id = "3", Label = "Afternoon" }
            );
            await context.SaveChangesAsync();

            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            // Act
            await service.ClearAllTimeSlotsAsync();

            // Assert
            var count = await context.TimeSlots.CountAsync();
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public async Task SaveAllTimeSlotsAsync_ClearsAndSavesAllTimeSlots()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            // Add some existing timeslots
            context.TimeSlots.AddRange(
                new TimeSlot { Id = "old-1", Label = "Old Slot 1" },
                new TimeSlot { Id = "old-2", Label = "Old Slot 2" }
            );
            await context.SaveChangesAsync();

            var logger = CreateLogger();
            var service = new LocationService(context, logger);

            var newTimeSlots = new List<TimeSlot>
            {
                new TimeSlot { Id = "new-1", Label = "New Morning", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(12, 0, 0) },
                new TimeSlot { Id = "new-2", Label = "New Afternoon", StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(17, 0, 0) }
            };

            // Act
            await service.SaveAllTimeSlotsAsync(newTimeSlots);

            // Assert
            var allSlots = await context.TimeSlots.ToListAsync();
            Assert.AreEqual(2, allSlots.Count);
            Assert.IsTrue(allSlots.Any(t => t.Id == "new-1"));
            Assert.IsTrue(allSlots.Any(t => t.Id == "new-2"));
            Assert.IsFalse(allSlots.Any(t => t.Id == "old-1"));
            Assert.IsFalse(allSlots.Any(t => t.Id == "old-2"));
        }

        #endregion

        // Note: Unique constraint tests are omitted because EF Core InMemory provider
        // doesn't enforce unique indexes. These constraints are defined in ApplicationDbContext
        // and are enforced by SQLite in production.
    }
}
