// <copyright file="HomeStateServiceTests.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using WinterAdventurer.Data;
using WinterAdventurer.Library.Models;
using WinterAdventurer.Models;
using WinterAdventurer.Services;

namespace WinterAdventurer.Test.Services
{
    /// <summary>
    /// Tests for HomeStateService state management.
    /// Verifies state mutations, property access, and behavior.
    /// </summary>
    [TestClass]
    public class HomeStateServiceTests
    {
        private HomeStateService _stateService = null!;

        [TestInitialize]
        public void Setup()
        {
            _stateService = new HomeStateService();
        }

        [TestMethod]
        public void SetWorkshops_WithEmptyList_UpdatesWorkshops()
        {
            // Arrange
            var workshops = new List<Workshop>();

            // Act
            _stateService.SetWorkshops(workshops);

            // Assert
            Assert.AreEqual(0, _stateService.Workshops.Count);
        }

        [TestMethod]
        public void SetWorkshops_WithMultipleWorkshops_UpdatesWorkshops()
        {
            // Arrange
            var workshops = new List<Workshop>
            {
                CreateTestWorkshop("Pottery", "Jane Doe"),
                CreateTestWorkshop("Woodworking", "John Smith"),
            };

            // Act
            _stateService.SetWorkshops(workshops);

            // Assert
            Assert.AreEqual(2, _stateService.Workshops.Count);
            Assert.AreEqual("Pottery", _stateService.Workshops[0].Name);
            Assert.AreEqual("Woodworking", _stateService.Workshops[1].Name);
        }

        [TestMethod]
        public void SetWorkshops_WithNull_CreatesEmptyList()
        {
            // Act
            _stateService.SetWorkshops(null!);

            // Assert
            Assert.AreEqual(0, _stateService.Workshops.Count);
            Assert.IsNotNull(_stateService.Workshops);
        }

        [TestMethod]
        public void Workshops_ReturnsReadOnlyCollection()
        {
            // Arrange
            var workshops = new List<Workshop> { CreateTestWorkshop("Pottery", "Jane") };
            _stateService.SetWorkshops(workshops);

            // Act & Assert
            try
            {
                ((IList<Workshop>)_stateService.Workshops).Add(CreateTestWorkshop("New", "New"));
                Assert.Fail("Expected NotSupportedException");
            }
            catch (NotSupportedException)
            {
                // Expected
            }
        }

        [TestMethod]
        public void SetPeriods_WithMultiplePeriods_UpdatesPeriods()
        {
            // Arrange
            var periods = new List<Period>
            {
                new Period("M1") { DisplayName = "Morning" },
                new Period("A1") { DisplayName = "Afternoon" },
            };

            // Act
            _stateService.SetPeriods(periods);

            // Assert
            Assert.AreEqual(2, _stateService.Periods.Count);
            Assert.AreEqual("Morning", _stateService.Periods[0].DisplayName);
        }

        [TestMethod]
        public void Periods_IsNotReadOnly()
        {
            // Arrange
            _stateService.SetPeriods(new List<Period>());

            // Act - Should not throw
            _stateService.Periods.Add(new Period("M1") { DisplayName = "Morning" });

            // Assert
            Assert.AreEqual(1, _stateService.Periods.Count);
        }

        [TestMethod]
        public void SetTimeslots_WithMultipleTimeslots_UpdatesTimeslots()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "1", Label = "Morning", StartTime = TimeSpan.FromHours(9), IsPeriod = true },
                new () { Id = "2", Label = "Afternoon", StartTime = TimeSpan.FromHours(13), IsPeriod = true },
            };

            // Act
            _stateService.SetTimeslots(timeslots);

            // Assert
            Assert.AreEqual(2, _stateService.Timeslots.Count);
            Assert.AreEqual("Morning", _stateService.Timeslots[0].Label);
        }

        [TestMethod]
        public void SetTimeslots_WithNull_CreatesEmptyList()
        {
            // Act
            _stateService.SetTimeslots(null!);

            // Assert
            Assert.AreEqual(0, _stateService.Timeslots.Count);
        }

        [TestMethod]
        public void Timeslots_ReturnsReadOnlyCollection()
        {
            // Arrange
            _stateService.SetTimeslots(new List<TimeSlotViewModel>());

            // Act & Assert
            try
            {
                ((IList<TimeSlotViewModel>)_stateService.Timeslots).Add(
                    new TimeSlotViewModel { Id = "1", Label = "Test" });
                Assert.Fail("Expected NotSupportedException");
            }
            catch (NotSupportedException)
            {
                // Expected
            }
        }

        [TestMethod]
        public void UpdateTimeslotValidation_WithBothFlags_SetsCorrectly()
        {
            // Act
            _stateService.UpdateTimeslotValidation(hasOverlapping: true, hasUnconfigured: false);

            // Assert
            Assert.IsTrue(_stateService.HasOverlappingTimeslots);
            Assert.IsFalse(_stateService.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void UpdateTimeslotValidation_WithBothFalse_ClearsFlags()
        {
            // Arrange
            _stateService.UpdateTimeslotValidation(true, true);

            // Act
            _stateService.UpdateTimeslotValidation(false, false);

            // Assert
            Assert.IsFalse(_stateService.HasOverlappingTimeslots);
            Assert.IsFalse(_stateService.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void AddLocation_WithNewLocation_AddsToList()
        {
            // Arrange
            var location = new Location { Id = 1, Name = "Art Studio" };

            // Act
            _stateService.AddLocation(location);

            // Assert
            Assert.AreEqual(1, _stateService.AvailableLocations.Count);
            Assert.AreEqual("Art Studio", _stateService.AvailableLocations[0].Name);
        }

        [TestMethod]
        public void AddLocation_WithMultipleLocations_SortsAlphabetically()
        {
            // Arrange
            var locations = new[]
            {
                new Location { Name = "Zebra Room" },
                new Location { Name = "Apple Hall" },
                new Location { Name = "Maple Studio" },
            };

            // Act
            foreach (var loc in locations)
            {
                _stateService.AddLocation(loc);
            }

            // Assert
            Assert.AreEqual("Apple Hall", _stateService.AvailableLocations[0].Name);
            Assert.AreEqual("Maple Studio", _stateService.AvailableLocations[1].Name);
            Assert.AreEqual("Zebra Room", _stateService.AvailableLocations[2].Name);
        }

        [TestMethod]
        public void AddLocation_WithDuplicateName_IgnoresDuplicate()
        {
            // Arrange
            var location1 = new Location { Id = 1, Name = "Art Studio" };
            var location2 = new Location { Id = 2, Name = "Art Studio" };

            // Act
            _stateService.AddLocation(location1);
            _stateService.AddLocation(location2);

            // Assert
            Assert.AreEqual(1, _stateService.AvailableLocations.Count);
        }

        [TestMethod]
        public void AddLocation_WithNull_IgnoresNull()
        {
            // Act
            _stateService.AddLocation(null!);

            // Assert
            Assert.AreEqual(0, _stateService.AvailableLocations.Count);
        }

        [TestMethod]
        public void RemoveLocation_WithExistingLocation_RemovesFromList()
        {
            // Arrange
            _stateService.AddLocation(new Location { Name = "Art Studio" });
            _stateService.AddLocation(new Location { Name = "Woodshop" });

            // Act
            _stateService.RemoveLocation("Art Studio");

            // Assert
            Assert.AreEqual(1, _stateService.AvailableLocations.Count);
            Assert.AreEqual("Woodshop", _stateService.AvailableLocations[0].Name);
        }

        [TestMethod]
        public void RemoveLocation_WithCaseInsensitiveName_Removes()
        {
            // Arrange
            _stateService.AddLocation(new Location { Name = "Art Studio" });

            // Act
            _stateService.RemoveLocation("art studio");

            // Assert
            Assert.AreEqual(0, _stateService.AvailableLocations.Count);
        }

        [TestMethod]
        public void RemoveLocation_WithNonExistentLocation_DoesNothing()
        {
            // Arrange
            _stateService.AddLocation(new Location { Name = "Art Studio" });

            // Act
            _stateService.RemoveLocation("Nonexistent");

            // Assert
            Assert.AreEqual(1, _stateService.AvailableLocations.Count);
        }

        [TestMethod]
        public void AvailableLocations_ReturnsReadOnlyCollection()
        {
            // Arrange
            _stateService.AddLocation(new Location { Name = "Art Studio" });

            // Act & Assert
            try
            {
                ((IList<Location>)_stateService.AvailableLocations).Add(
                    new Location { Name = "New" });
                Assert.Fail("Expected NotSupportedException");
            }
            catch (NotSupportedException)
            {
                // Expected
            }
        }

        [TestMethod]
        public void IncrementTimeslotVersion_IncrementsCounter()
        {
            // Arrange
            var initialVersion = _stateService.TimeslotVersion;

            // Act
            _stateService.IncrementTimeslotVersion();

            // Assert
            Assert.AreEqual(initialVersion + 1, _stateService.TimeslotVersion);
        }

        [TestMethod]
        public void IncrementTimeslotVersion_MultipleIncrements_CumulativelyIncreases()
        {
            // Arrange
            var initialVersion = _stateService.TimeslotVersion;

            // Act
            _stateService.IncrementTimeslotVersion();
            _stateService.IncrementTimeslotVersion();
            _stateService.IncrementTimeslotVersion();

            // Assert
            Assert.AreEqual(initialVersion + 3, _stateService.TimeslotVersion);
        }

        [TestMethod]
        public void IncrementLocationListVersion_IncrementsCounter()
        {
            // Arrange
            var initialVersion = _stateService.LocationListVersion;

            // Act
            _stateService.IncrementLocationListVersion();

            // Assert
            Assert.AreEqual(initialVersion + 1, _stateService.LocationListVersion);
        }

        [TestMethod]
        public void IsLoading_CanBeSetAndRetrieved()
        {
            // Act & Assert
            Assert.IsFalse(_stateService.IsLoading);

            _stateService.IsLoading = true;
            Assert.IsTrue(_stateService.IsLoading);

            _stateService.IsLoading = false;
            Assert.IsFalse(_stateService.IsLoading);
        }

        [TestMethod]
        public void Success_CanBeSetAndRetrieved()
        {
            // Act & Assert
            Assert.IsFalse(_stateService.Success);

            _stateService.Success = true;
            Assert.IsTrue(_stateService.Success);
        }

        [TestMethod]
        public void ShowPdfSuccess_CanBeSetAndRetrieved()
        {
            // Act & Assert
            Assert.IsFalse(_stateService.ShowPdfSuccess);

            _stateService.ShowPdfSuccess = true;
            Assert.IsTrue(_stateService.ShowPdfSuccess);
        }

        [TestMethod]
        public void ErrorMessage_CanBeSetAndRetrieved()
        {
            // Act & Assert
            Assert.IsNull(_stateService.ErrorMessage);

            _stateService.ErrorMessage = "Test error";
            Assert.AreEqual("Test error", _stateService.ErrorMessage);

            _stateService.ErrorMessage = null;
            Assert.IsNull(_stateService.ErrorMessage);
        }

        [TestMethod]
        public void EventName_CanBeSetAndRetrieved()
        {
            // Act & Assert
            Assert.AreEqual(string.Empty, _stateService.EventName);

            _stateService.EventName = "Winter Adventure 2024";
            Assert.AreEqual("Winter Adventure 2024", _stateService.EventName);
        }

        [TestMethod]
        public void BlankScheduleCount_CanBeSetAndRetrieved()
        {
            // Act & Assert
            Assert.AreEqual(0, _stateService.BlankScheduleCount);

            _stateService.BlankScheduleCount = 5;
            Assert.AreEqual(5, _stateService.BlankScheduleCount);
        }

        [TestMethod]
        public void ClearAll_ResetsAllState()
        {
            // Arrange - Set all state
            _stateService.SetWorkshops(new List<Workshop> { CreateTestWorkshop("Test", "Test") });
            _stateService.SetTimeslots(new List<TimeSlotViewModel> { new () { Label = "Test" } });
            _stateService.AddLocation(new Location { Name = "Test" });
            _stateService.UpdateTimeslotValidation(true, true);
            _stateService.IsLoading = true;
            _stateService.Success = true;
            _stateService.ShowPdfSuccess = true;
            _stateService.ErrorMessage = "Test error";
            _stateService.EventName = "Test Event";
            _stateService.BlankScheduleCount = 5;

            // Act
            _stateService.ClearAll();

            // Assert
            Assert.AreEqual(0, _stateService.Workshops.Count);
            Assert.AreEqual(0, _stateService.Timeslots.Count);
            Assert.AreEqual(0, _stateService.AvailableLocations.Count);
            Assert.IsFalse(_stateService.HasOverlappingTimeslots);
            Assert.IsFalse(_stateService.HasUnconfiguredTimeslots);
            Assert.IsFalse(_stateService.IsLoading);
            Assert.IsFalse(_stateService.ShowPdfSuccess);
            Assert.IsNull(_stateService.ErrorMessage);

            // Note: Success, EventName and BlankScheduleCount are NOT cleared by ClearAll
            Assert.IsTrue(_stateService.Success);
            Assert.AreEqual("Test Event", _stateService.EventName);
            Assert.AreEqual(5, _stateService.BlankScheduleCount);
        }

        private Workshop CreateTestWorkshop(string name, string leader)
        {
            return new Workshop
            {
                Name = name,
                Leader = leader,
                Period = new Period("MorningFirstPeriod") { DisplayName = "Morning First Period" },
                Duration = new WorkshopDuration(1, 4),
                Selections = new List<WorkshopSelection>
                {
                    new ()
                    {
                        ClassSelectionId = "SEL001",
                        FirstName = "Test",
                        LastName = "Person",
                        ChoiceNumber = 1,
                    },
                },
            };
        }
    }
}
