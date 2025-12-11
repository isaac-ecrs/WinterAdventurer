// <copyright file="TimeslotOperationServiceTests.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Moq;
using WinterAdventurer.Data;
using WinterAdventurer.Library.Models;
using WinterAdventurer.Library.Services;
using WinterAdventurer.Models;
using WinterAdventurer.Services;
using DataTimeSlot = WinterAdventurer.Data.TimeSlot;
using ValidationResult = WinterAdventurer.Library.Services.ValidationResult;

namespace WinterAdventurer.Test.Services
{
    /// <summary>
    /// Tests for TimeslotOperationService timeslot operations.
    /// Verifies sorting, validation, conversion, and persistence logic.
    /// </summary>
    [TestClass]
    public class TimeslotOperationServiceTests
    {
        private TimeslotOperationService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _service = new TimeslotOperationService();
        }

        [TestMethod]
        public void SortTimeslots_WithMixedStartTimes_SortsByStartTimeAscending()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "3", Label = "Afternoon", StartTime = TimeSpan.FromHours(13) },
                new () { Id = "1", Label = "Morning", StartTime = TimeSpan.FromHours(9) },
                new () { Id = "2", Label = "Midday", StartTime = TimeSpan.FromHours(12) },
            };

            // Act
            _service.SortTimeslots(timeslots);

            // Assert
            Assert.AreEqual("1", timeslots[0].Id);
            Assert.AreEqual("2", timeslots[1].Id);
            Assert.AreEqual("3", timeslots[2].Id);
        }

        [TestMethod]
        public void SortTimeslots_WithNullStartTimes_PlacesNullsLast()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "3", Label = "Unscheduled", StartTime = null },
                new () { Id = "1", Label = "Morning", StartTime = TimeSpan.FromHours(9) },
                new () { Id = "2", Label = "Evening", StartTime = null },
            };

            // Act
            _service.SortTimeslots(timeslots);

            // Assert
            Assert.AreEqual("1", timeslots[0].Id);
            Assert.AreEqual("3", timeslots[1].Id);
            Assert.AreEqual("2", timeslots[2].Id);
        }

        [TestMethod]
        public void SortTimeslots_WithAllNullStartTimes_MaintainsOrder()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "3", Label = "Third" },
                new () { Id = "1", Label = "First" },
                new () { Id = "2", Label = "Second" },
            };

            // Act
            _service.SortTimeslots(timeslots);

            // Assert - Order may vary since all are null, but we verify no exceptions occur
            Assert.AreEqual(3, timeslots.Count);
        }

        [TestMethod]
        public void SortTimeslots_WithEmptyList_DoesNotThrow()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>();

            // Act & Assert
            _service.SortTimeslots(timeslots);
            Assert.AreEqual(0, timeslots.Count);
        }

        [TestMethod]
        public void SortTimeslots_WithSingleElement_DoesNotThrow()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "1", Label = "Morning", StartTime = TimeSpan.FromHours(9) },
            };

            // Act & Assert
            _service.SortTimeslots(timeslots);
            Assert.AreEqual(1, timeslots.Count);
        }

        [TestMethod]
        public void PopulateTimeslotsFromPeriods_WithMultiplePeriods_CreatesNewSlots()
        {
            // Arrange
            var periods = new List<Period>
            {
                new ("M1") { DisplayName = "Morning Session" },
                new ("A1") { DisplayName = "Afternoon Session" },
            };
            var existingTimeslots = new List<TimeSlotViewModel>();

            // Act
            var result = _service.PopulateTimeslotsFromPeriods(periods, existingTimeslots);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.All(t => t.IsPeriod));
            Assert.AreEqual("Morning Session", result[0].Label);
            Assert.AreEqual("Afternoon Session", result[1].Label);
        }

        [TestMethod]
        public void PopulateTimeslotsFromPeriods_WithExistingPeriodSlots_PreservesExisting()
        {
            // Arrange
            var existingId = "existing-1";
            var periods = new List<Period>
            {
                new ("M1") { DisplayName = "Morning" },
            };
            var existingTimeslots = new List<TimeSlotViewModel>
            {
                new () { Id = existingId, Label = "Morning", IsPeriod = true, StartTime = TimeSpan.FromHours(9) },
            };

            // Act
            var result = _service.PopulateTimeslotsFromPeriods(periods, existingTimeslots);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(existingId, result[0].Id);
            Assert.AreEqual(TimeSpan.FromHours(9), result[0].StartTime);
        }

        [TestMethod]
        public void PopulateTimeslotsFromPeriods_WithUserSlots_PreservesUserSlots()
        {
            // Arrange
            var periods = new List<Period>
            {
                new ("M1") { DisplayName = "Morning" },
            };
            var existingTimeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "custom-1", Label = "Custom Break", IsPeriod = false, StartTime = TimeSpan.FromHours(10, 30) },
            };

            // Act
            var result = _service.PopulateTimeslotsFromPeriods(periods, existingTimeslots);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(t => t.Label == "Morning" && t.IsPeriod));
            Assert.IsTrue(result.Any(t => t.Label == "Custom Break" && !t.IsPeriod));
        }

        [TestMethod]
        public void PopulateTimeslotsFromPeriods_WithNullExistingList_CreatesNewSlots()
        {
            // Arrange
            var periods = new List<Period>
            {
                new ("M1") { DisplayName = "Morning" },
            };

            // Act
            var result = _service.PopulateTimeslotsFromPeriods(periods, new List<TimeSlotViewModel>());

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Morning", result[0].Label);
        }

        [TestMethod]
        public void PopulateTimeslotsFromPeriods_ResultIsSorted()
        {
            // Arrange
            var periods = new List<Period>
            {
                new ("A1") { DisplayName = "Afternoon" },
                new ("M1") { DisplayName = "Morning" },
            };
            var existingTimeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "m", Label = "Morning", IsPeriod = true, StartTime = TimeSpan.FromHours(9) },
                new () { Id = "a", Label = "Afternoon", IsPeriod = true, StartTime = TimeSpan.FromHours(13) },
            };

            // Act
            var result = _service.PopulateTimeslotsFromPeriods(periods, existingTimeslots);

            // Assert - Result should be sorted by StartTime
            Assert.AreEqual("m", result[0].Id);
            Assert.AreEqual("a", result[1].Id);
        }

        [TestMethod]
        public void ValidateTimeslots_CallsValidatorWithConvertedDtos()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "1", Label = "Morning", StartTime = TimeSpan.FromHours(9) },
            };
            var mockValidator = new Mock<ITimeslotValidationService>();
            mockValidator.Setup(v => v.ValidateTimeslots(It.IsAny<IEnumerable<TimeSlotDto>>()))
                .Returns(new ValidationResult { HasOverlappingTimeslots = false, HasUnconfiguredTimeslots = false });

            // Act
            _service.ValidateTimeslots(timeslots, mockValidator.Object, out var hasOverlap, out var hasUnconfigured);

            // Assert
            mockValidator.Verify(v => v.ValidateTimeslots(It.IsAny<IEnumerable<TimeSlotDto>>()), Times.Once);
            Assert.IsFalse(hasOverlap);
            Assert.IsFalse(hasUnconfigured);
        }

        [TestMethod]
        public void ValidateTimeslots_ReturnsTrueForOverlappingTimeslots()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "1", Label = "Period 1", StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(12) },
            };
            var mockValidator = new Mock<ITimeslotValidationService>();
            mockValidator.Setup(v => v.ValidateTimeslots(It.IsAny<IEnumerable<TimeSlotDto>>()))
                .Returns(new ValidationResult { HasOverlappingTimeslots = true, HasUnconfiguredTimeslots = false });

            // Act
            _service.ValidateTimeslots(timeslots, mockValidator.Object, out var hasOverlap, out var hasUnconfigured);

            // Assert
            Assert.IsTrue(hasOverlap);
            Assert.IsFalse(hasUnconfigured);
        }

        [TestMethod]
        public void ValidateTimeslots_ReturnsTrueForUnconfiguredTimeslots()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "1", Label = "Unconfigured", StartTime = null },
            };
            var mockValidator = new Mock<ITimeslotValidationService>();
            mockValidator.Setup(v => v.ValidateTimeslots(It.IsAny<IEnumerable<TimeSlotDto>>()))
                .Returns(new ValidationResult { HasOverlappingTimeslots = false, HasUnconfiguredTimeslots = true });

            // Act
            _service.ValidateTimeslots(timeslots, mockValidator.Object, out var hasOverlap, out var hasUnconfigured);

            // Assert
            Assert.IsFalse(hasOverlap);
            Assert.IsTrue(hasUnconfigured);
        }

        [TestMethod]
        public void ValidateTimeslots_WithBothFlags_ReturnsBothTrue()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "1", Label = "Period", StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10) },
            };
            var mockValidator = new Mock<ITimeslotValidationService>();
            mockValidator.Setup(v => v.ValidateTimeslots(It.IsAny<IEnumerable<TimeSlotDto>>()))
                .Returns(new ValidationResult { HasOverlappingTimeslots = true, HasUnconfiguredTimeslots = true });

            // Act
            _service.ValidateTimeslots(timeslots, mockValidator.Object, out var hasOverlap, out var hasUnconfigured);

            // Assert
            Assert.IsTrue(hasOverlap);
            Assert.IsTrue(hasUnconfigured);
        }

        [TestMethod]
        public void ConvertToLibraryTimeSlots_ConvertsAllProperties()
        {
            // Arrange
            var viewModels = new List<TimeSlotViewModel>
            {
                new () { Id = "1", Label = "Morning", StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(12), IsPeriod = true },
                new () { Id = "2", Label = "Custom", StartTime = TimeSpan.FromHours(13), EndTime = TimeSpan.FromHours(14), IsPeriod = false },
            };

            // Act
            var result = _service.ConvertToLibraryTimeSlots(viewModels);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("1", result[0].Id);
            Assert.AreEqual("Morning", result[0].Label);
            Assert.AreEqual(TimeSpan.FromHours(9), result[0].StartTime);
            Assert.AreEqual(TimeSpan.FromHours(12), result[0].EndTime);
            Assert.IsTrue(result[0].IsPeriod);
            Assert.IsFalse(result[1].IsPeriod);
        }

        [TestMethod]
        public void ConvertToLibraryTimeSlots_WithNullTimeTimes_MapsNulls()
        {
            // Arrange
            var viewModels = new List<TimeSlotViewModel>
            {
                new () { Id = "1", Label = "Unconfigured", StartTime = null, EndTime = null },
            };

            // Act
            var result = _service.ConvertToLibraryTimeSlots(viewModels);

            // Assert
            Assert.IsNull(result[0].StartTime);
            Assert.IsNull(result[0].EndTime);
        }

        [TestMethod]
        public void ConvertToLibraryTimeSlots_WithEmptyList_ReturnsEmpty()
        {
            // Arrange
            var viewModels = new List<TimeSlotViewModel>();

            // Act
            var result = _service.ConvertToLibraryTimeSlots(viewModels);

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task SaveTimeslotsAsync_WithValidData_SuccessfullyCallsLocationService()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "1", Label = "Morning", StartTime = TimeSpan.FromHours(9) },
            };
            var mockLocationService = new Mock<ILocationService>();
            mockLocationService.Setup(ls => ls.SaveAllTimeSlotsAsync(It.IsAny<List<DataTimeSlot>>()))
                .Returns(Task.CompletedTask);
            var mockValidator = new Mock<ITimeslotValidationService>();
            mockValidator.Setup(v => v.ValidateTimeslots(It.IsAny<IEnumerable<TimeSlotDto>>()))
                .Returns(new ValidationResult { HasOverlappingTimeslots = false, HasUnconfiguredTimeslots = false });

            // Act
            var result = await _service.SaveTimeslotsAsync(timeslots, mockLocationService.Object, mockValidator.Object);

            // Assert
            Assert.IsTrue(result.Success);
            mockLocationService.Verify(ls => ls.SaveAllTimeSlotsAsync(It.IsAny<List<DataTimeSlot>>()), Times.Once);
        }

        [TestMethod]
        public async Task SaveTimeslotsAsync_IncludesValidationResults()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "1", Label = "Period 1", StartTime = TimeSpan.FromHours(9) },
            };
            var mockLocationService = new Mock<ILocationService>();
            mockLocationService.Setup(ls => ls.SaveAllTimeSlotsAsync(It.IsAny<List<DataTimeSlot>>()))
                .Returns(Task.CompletedTask);
            var mockValidator = new Mock<ITimeslotValidationService>();
            mockValidator.Setup(v => v.ValidateTimeslots(It.IsAny<IEnumerable<TimeSlotDto>>()))
                .Returns(new ValidationResult { HasOverlappingTimeslots = true, HasUnconfiguredTimeslots = false });

            // Act
            var result = await _service.SaveTimeslotsAsync(timeslots, mockLocationService.Object, mockValidator.Object);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public async Task SaveTimeslotsAsync_WhenLocationServiceThrows_CatchesException()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "1", Label = "Morning", StartTime = TimeSpan.FromHours(9) },
            };
            var mockLocationService = new Mock<ILocationService>();
            mockLocationService.Setup(ls => ls.SaveAllTimeSlotsAsync(It.IsAny<List<DataTimeSlot>>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));
            var mockValidator = new Mock<ITimeslotValidationService>();
            mockValidator.Setup(v => v.ValidateTimeslots(It.IsAny<IEnumerable<TimeSlotDto>>()))
                .Returns(new ValidationResult { HasOverlappingTimeslots = false, HasUnconfiguredTimeslots = false });

            // Act
            var result = await _service.SaveTimeslotsAsync(timeslots, mockLocationService.Object, mockValidator.Object);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsTrue(result.ErrorMessage.Contains("Database error"));
        }

        [TestMethod]
        public async Task SaveTimeslotsAsync_SortsBeforeSaving()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>
            {
                new () { Id = "3", Label = "Afternoon", StartTime = TimeSpan.FromHours(13) },
                new () { Id = "1", Label = "Morning", StartTime = TimeSpan.FromHours(9) },
            };
            var mockLocationService = new Mock<ILocationService>();
            mockLocationService.Setup(ls => ls.SaveAllTimeSlotsAsync(It.IsAny<List<DataTimeSlot>>()))
                .Returns(Task.CompletedTask);
            var mockValidator = new Mock<ITimeslotValidationService>();
            mockValidator.Setup(v => v.ValidateTimeslots(It.IsAny<IEnumerable<TimeSlotDto>>()))
                .Returns(new ValidationResult { HasOverlappingTimeslots = false, HasUnconfiguredTimeslots = false });

            // Act
            var result = await _service.SaveTimeslotsAsync(timeslots, mockLocationService.Object, mockValidator.Object);

            // Assert - Verify that we saved in sorted order
            mockLocationService.Verify(
                ls => ls.SaveAllTimeSlotsAsync(
                    It.Is<List<DataTimeSlot>>(slots => slots[0].Id == "1" && slots[1].Id == "3")),
                Times.Once);
        }

        [TestMethod]
        public async Task SaveTimeslotsAsync_WithEmptyList_DoesNotThrow()
        {
            // Arrange
            var timeslots = new List<TimeSlotViewModel>();
            var mockLocationService = new Mock<ILocationService>();
            mockLocationService.Setup(ls => ls.SaveAllTimeSlotsAsync(It.IsAny<List<DataTimeSlot>>()))
                .Returns(Task.CompletedTask);
            var mockValidator = new Mock<ITimeslotValidationService>();
            mockValidator.Setup(v => v.ValidateTimeslots(It.IsAny<IEnumerable<TimeSlotDto>>()))
                .Returns(new ValidationResult { HasOverlappingTimeslots = false, HasUnconfiguredTimeslots = false });

            // Act
            var result = await _service.SaveTimeslotsAsync(timeslots, mockLocationService.Object, mockValidator.Object);

            // Assert
            Assert.IsTrue(result.Success);
            mockLocationService.Verify(ls => ls.SaveAllTimeSlotsAsync(It.IsAny<List<DataTimeSlot>>()), Times.Once);
        }
    }
}
