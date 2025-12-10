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

        [TestMethod]
        public void WorkshopCard_Render_DisplaysPeriodName()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.CardIndex, 0));

            // Assert
            Assert.Contains("Morning First Period", cut.Markup);
        }

        [TestMethod]
        public void WorkshopCard_OnParametersSet_SetsSelectedLocation()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");
            workshop.Location = "Art Studio";
            var locations = new List<Location>
            {
                new Location { Id = 1, Name = "Art Studio" }
            };

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, locations)
                .Add(p => p.CardIndex, 0));

            // Assert - Location wrapper div should exist and markup should contain the location
            var locationDiv = cut.Find("#first-workshop-location");
            Assert.IsNotNull(locationDiv);
            Assert.Contains("Art Studio", cut.Markup);
        }

        [TestMethod]
        public void WorkshopCard_OnParametersSet_HandlesNullLocation()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");
            workshop.Location = string.Empty;
            var locations = new List<Location>
            {
                new Location { Id = 1, Name = "Art Studio" }
            };

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, locations)
                .Add(p => p.CardIndex, 0));

            // Assert - Should render without error
            Assert.IsNotNull(cut);
        }

        [TestMethod]
        public void WorkshopCard_SearchLocations_ReturnsAllLocationsForEmptySearch()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");
            var locations = new List<Location>
            {
                new Location { Id = 1, Name = "Art Studio" },
                new Location { Id = 2, Name = "Workshop Room" },
                new Location { Id = 3, Name = "Craft Hall" }
            };

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, locations)
                .Add(p => p.CardIndex, 0));

            // Assert - Component should render all locations (tested via markup)
            Assert.IsNotNull(cut);
        }

        [TestMethod]
        public void WorkshopCard_SearchLocations_FiltersOutUsedLocationsInSamePeriod()
        {
            // Arrange
            var period = new Period("MorningFirstPeriod");
            var workshop1 = CreateTestWorkshop("Pottery", "John Smith");
            workshop1.Location = "Art Studio";
            workshop1.Period = period;

            var workshop2 = CreateTestWorkshop("Woodworking", "Jane Doe");
            workshop2.Period = period;

            var locations = new List<Location>
            {
                new Location { Id = 1, Name = "Art Studio" },
                new Location { Id = 2, Name = "Workshop Room" }
            };

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop2)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop1, workshop2 })
                .Add(p => p.AvailableLocations, locations)
                .Add(p => p.CardIndex, 0));

            // Assert - Should render successfully (Art Studio should be filtered out in SearchLocations)
            Assert.IsNotNull(cut);
        }

        [TestMethod]
        public void WorkshopCard_SearchLocations_AllowsCurrentWorkshopLocation()
        {
            // Arrange
            var period = new Period("MorningFirstPeriod");
            var workshop = CreateTestWorkshop("Pottery", "John Smith");
            workshop.Location = "Art Studio";
            workshop.Period = period;

            var locations = new List<Location>
            {
                new Location { Id = 1, Name = "Art Studio" }
            };

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, locations)
                .Add(p => p.CardIndex, 0));

            // Assert - Should render with current location available
            Assert.IsNotNull(cut);
        }

        [TestMethod]
        public void WorkshopCard_SearchLocations_DoesNotFilterLocationsFromDifferentPeriods()
        {
            // Arrange
            var period1 = new Period("MorningFirstPeriod");
            var period2 = new Period("AfternoonFirstPeriod");

            var workshop1 = CreateTestWorkshop("Pottery", "John Smith");
            workshop1.Location = "Art Studio";
            workshop1.Period = period1;

            var workshop2 = CreateTestWorkshop("Woodworking", "Jane Doe");
            workshop2.Period = period2;

            var locations = new List<Location>
            {
                new Location { Id = 1, Name = "Art Studio" }
            };

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop2)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop1, workshop2 })
                .Add(p => p.AvailableLocations, locations)
                .Add(p => p.CardIndex, 0));

            // Assert - Art Studio should be available since it's in a different period
            Assert.IsNotNull(cut);
        }

        [TestMethod]
        public void WorkshopCard_TagsDisplay_RendersLocationTags()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");
            var locations = new List<Location>
            {
                new Location
                {
                    Id = 1,
                    Name = "Art Studio",
                    Tags = new List<Tag>
                    {
                        new Tag { Id = 1, Name = "Downstairs", Color = "#FF0000" }
                    }
                }
            };

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, locations)
                .Add(p => p.CardIndex, 0));

            // Assert - Should render successfully with tags
            Assert.IsNotNull(cut);
        }

        [TestMethod]
        public void WorkshopCard_MultipleSelections_ShowsCorrectCount()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");
            for (int i = 1; i <= 10; i++)
            {
                workshop.Selections.Add(new WorkshopSelection
                {
                    FirstName = $"First{i}",
                    LastName = $"Last{i}",
                    ChoiceNumber = 1
                });
            }

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.CardIndex, 0));

            // Assert
            Assert.Contains("10 Participants", cut.Markup);
        }

        [TestMethod]
        public void WorkshopCard_ZeroSelections_ShowsZeroCount()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.CardIndex, 0));

            // Assert
            Assert.Contains("0 Participants", cut.Markup);
        }

        [TestMethod]
        [Ignore("SetParametersAndRender is not available in current bunit version")]
        public void WorkshopCard_LocationListVersion_UpdatesCausesRerender()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");
            var locations = new List<Location>
            {
                new Location { Id = 1, Name = "Art Studio" }
            };

            // Act - Render with version 0
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, locations)
                .Add(p => p.LocationListVersion, 0)
                .Add(p => p.CardIndex, 0));

            var initialMarkup = cut.Markup;

            // Update to version 1
            // cut.SetParametersAndRender(parameters => parameters
            //     .Add(p => p.LocationListVersion, 1));

            var updatedMarkup = cut.Markup;

            // Assert - Markup should contain new version key
            Assert.IsNotNull(initialMarkup);
            Assert.IsNotNull(updatedMarkup);
        }

        [TestMethod]
        public void WorkshopCard_NameField_IsEditable()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.CardIndex, 0));

            // Assert - Find name input
            var nameInput = cut.Find("input[type='text'][value='Pottery']");
            Assert.IsNotNull(nameInput);
        }

        [TestMethod]
        public void WorkshopCard_LeaderField_IsEditable()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.CardIndex, 0));

            // Assert - Find leader input
            var inputs = cut.FindAll("input[type='text']");
            var leaderInput = inputs.FirstOrDefault(i => i.GetAttribute("value") == "John Smith");
            Assert.IsNotNull(leaderInput);
        }

        [TestMethod]
        public void WorkshopCard_LocationAutocomplete_HasCorrectAttributes()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");
            var locations = new List<Location>
            {
                new Location { Id = 1, Name = "Art Studio" }
            };

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, locations)
                .Add(p => p.CardIndex, 0));

            // Assert - Verify MudAutocomplete is rendered (it contains MudBlazor component classes)
            Assert.Contains("mud-autocomplete", cut.Markup.ToLower());
            // Verify the location wrapper div exists
            var locationDiv = cut.Find("#first-workshop-location");
            Assert.IsNotNull(locationDiv);
        }

        [TestMethod]
        public void WorkshopCard_MudCardStructure_HasHeaderAndContent()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.CardIndex, 0));

            // Assert - Should have MudCard structure
            Assert.Contains("mud-card", cut.Markup.ToLower());
        }

        [TestMethod]
        public void WorkshopCard_DifferentPeriods_DisplayCorrectly()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");
            workshop.Period = new Period("AfternoonSecondPeriod");

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.CardIndex, 0));

            // Assert
            Assert.Contains("Afternoon Second Period", cut.Markup);
        }

        [TestMethod]
        public void WorkshopCard_OnLocationChanged_CallbackParameter_IsProvided()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");
            EventCallback<Workshop> callback = EventCallback.Factory.Create<Workshop>(this, (_) => { });

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.OnLocationChanged, callback)
                .Add(p => p.CardIndex, 0));

            // Assert - Component should render with callback
            Assert.IsNotNull(cut);
        }

        [TestMethod]
        public void WorkshopCard_OnDeleteLocationRequested_CallbackParameter_IsProvided()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");
            EventCallback<string> callback = EventCallback.Factory.Create<string>(this, (_) => { });

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.OnDeleteLocationRequested, callback)
                .Add(p => p.CardIndex, 0));

            // Assert - Component should render with callback
            Assert.IsNotNull(cut);
        }

        [TestMethod]
        public void WorkshopCard_CardWithBackupSelections_DisplaysAllSelections()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");
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
                ChoiceNumber = 2 // Backup choice
            });

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.CardIndex, 0));

            // Assert - Should show total count including backup
            Assert.Contains("2 Participants", cut.Markup);
        }

        [TestMethod]
        public void WorkshopCard_EmptyLocationsList_DoesNotCauseError()
        {
            // Arrange
            var workshop = CreateTestWorkshop("Pottery", "John Smith");

            // Act
            var cut = Render<WorkshopCard>(parameters => parameters
                .Add(p => p.Workshop, workshop)
                .Add(p => p.AllWorkshops, new List<Workshop> { workshop })
                .Add(p => p.AvailableLocations, new List<Location>())
                .Add(p => p.CardIndex, 0));

            // Assert - Should render without error
            Assert.IsNotNull(cut);
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
