using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MudBlazor;
using MudBlazor.Services;
using WinterAdventurer.Components.Shared;
using WinterAdventurer.Data;
using WinterAdventurer.Library.Models;
using WinterAdventurer.Services;
using BunitTestContext = Bunit.BunitContext;
using DataTimeSlot = WinterAdventurer.Data.TimeSlot;

namespace WinterAdventurer.Test.Components
{
    /// <summary>
    /// Tests for WorkshopCard Blazor component.
    /// Validates rendering, user interactions, and data binding.
    /// </summary>
    [TestClass]
    public class WorkshopCardTests : BunitTestContext
    {
        [TestInitialize]
        public void Setup()
        {
            // Register required services
            Services.AddMudServices(config =>
            {
                config.PopoverOptions.ThrowOnDuplicateProvider = false;
            });
            Services.AddSingleton<ILocationService, MockLocationService>();
            Services.AddSingleton(NullLogger<WorkshopCard>.Instance);

            // Configure JSInterop for MudBlazor components
            JSInterop.Mode = JSRuntimeMode.Loose;

            // Render MudPopoverProvider once for all tests
            Render<MudPopoverProvider>();
        }

        [TestMethod]
        public void WorkshopCard_Render_DisplaysWorkshopName()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");
            var locations = new List<Location>();

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, locations)
                .Add(p => p.CardIndex, 0));

            // Assert
            var nameInput = cut.Find("input[type='text']");
            Assert.IsNotNull(nameInput);
            Assert.AreEqual("Pottery", nameInput.GetAttribute("value"));
        }

        [TestMethod]
        public void WorkshopCard_Render_DisplaysParticipantCount()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Woodworking", "Jane Doe");
            workshop.Selections.Add(new WorkshopSelection
            {
                FirstName = "Alice",
                LastName = "Johnson",
                ChoiceNumber = 1
            });
            workshop.Selections.Add(new WorkshopSelection
            {
                FirstName = "Bob",
                LastName = "Smith",
                ChoiceNumber = 1
            });

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.CardIndex, 0));

            // Assert
            var participantText = cut.Find("p:contains('Participants')");
            Assert.Contains("2 Participants", participantText.TextContent);
        }

        [TestMethod]
        public void WorkshopCard_FirstCard_HasCorrectIds()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.CardIndex, 0));

            // Assert - Verify first workshop has special IDs for tour
            var locationDiv = cut.Find("#first-workshop-location");
            Assert.IsNotNull(locationDiv, "First workshop should have #first-workshop-location ID");

            var leaderDiv = cut.Find("#first-workshop-leader");
            Assert.IsNotNull(leaderDiv, "First workshop should have #first-workshop-leader ID");

            // Verify card-index data attributes
            Assert.AreEqual("0", locationDiv.GetAttribute("data-card-index"));
            Assert.AreEqual("0", leaderDiv.GetAttribute("data-card-index"));
        }

        [TestMethod]
        public void WorkshopCard_NonFirstCard_DoesNotHaveSpecialIds()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.CardIndex, 1)); // Not first card

            // Assert - Second workshop should NOT have special IDs
            var locationDivs = cut.FindAll("#first-workshop-location");
            Assert.IsEmpty(locationDivs, "Non-first workshop should not have #first-workshop-location ID");

            var leaderDivs = cut.FindAll("#first-workshop-leader");
            Assert.IsEmpty(leaderDivs, "Non-first workshop should not have #first-workshop-leader ID");
        }

        [TestMethod]
        public void WorkshopCard_LeaderField_BindsCorrectly()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "Jane Doe");

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.CardIndex, 0));

            // Assert - Find leader input and verify value
            var inputs = cut.FindAll("input[type='text']");
            var leaderInput = inputs.FirstOrDefault(i => i.GetAttribute("value") == "Jane Doe");

            Assert.IsNotNull(leaderInput, "Leader input should exist with correct value");
        }

        /// <summary>
        /// Helper method to create a test workshop with minimal data.
        /// </summary>
        private Workshop CreateTestWorkshop(string name, string leader)
        {
            return new Workshop
            {
                Name = name,
                Leader = leader,
                Period = new Period("MorningFirstPeriod"),
                Duration = new WorkshopDuration(1, 4),
                Selections = new List<WorkshopSelection>()
            };
        }
    }

    /// <summary>
    /// Mock implementation of ILocationService for component testing.
    /// Returns predefined locations without database access.
    /// </summary>
    internal class MockLocationService : ILocationService
    {
        private readonly List<Location> _locations = new();
        private readonly Dictionary<string, string> _workshopMappings = new();

        public Task<List<string>> GetAllLocationNamesAsync()
        {
            return Task.FromResult(_locations.Select(l => l.Name).ToList());
        }

        public Task<List<Location>> GetAllLocationsWithTagsAsync()
        {
            return Task.FromResult(_locations.ToList());
        }

        public Task<Location?> GetLocationByNameAsync(string name)
        {
            return Task.FromResult(_locations.FirstOrDefault(l => l.Name == name));
        }

        public Task<Location> AddOrGetLocationAsync(string locationName)
        {
            var existing = _locations.FirstOrDefault(l => l.Name == locationName);
            if (existing != null) return Task.FromResult(existing);

            var newLocation = new Location { Id = _locations.Count + 1, Name = locationName };
            _locations.Add(newLocation);
            return Task.FromResult(newLocation);
        }

        public Task<bool> DeleteLocationAsync(string locationName)
        {
            var location = _locations.FirstOrDefault(l => l.Name == locationName);
            if (location == null) return Task.FromResult(false);

            _locations.Remove(location);
            return Task.FromResult(true);
        }

        public Task<string?> GetWorkshopLocationMappingAsync(string workshopName)
        {
            _workshopMappings.TryGetValue(workshopName, out var location);
            return Task.FromResult(location);
        }

        public Task SaveWorkshopLocationMappingAsync(string workshopName, string locationName)
        {
            _workshopMappings[workshopName] = locationName;
            return Task.CompletedTask;
        }

        public Task<Dictionary<string, string>> GetAllWorkshopLocationMappingsAsync()
        {
            return Task.FromResult(new Dictionary<string, string>(_workshopMappings));
        }

        public Task<List<DataTimeSlot>> GetAllTimeSlotsAsync()
        {
            return Task.FromResult(new List<DataTimeSlot>());
        }

        public Task<DataTimeSlot?> GetTimeSlotByIdAsync(string id)
        {
            return Task.FromResult<DataTimeSlot?>(null);
        }

        public Task<DataTimeSlot> SaveTimeSlotAsync(DataTimeSlot timeSlot)
        {
            return Task.FromResult(timeSlot);
        }

        public Task<bool> DeleteTimeSlotAsync(string id)
        {
            return Task.FromResult(true);
        }

        public Task ClearAllTimeSlotsAsync()
        {
            return Task.CompletedTask;
        }

        public Task SaveAllTimeSlotsAsync(List<DataTimeSlot> timeSlots)
        {
            return Task.CompletedTask;
        }
    }
}
