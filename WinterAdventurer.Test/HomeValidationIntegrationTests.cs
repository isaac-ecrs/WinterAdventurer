using WinterAdventurer.Library.Models;
using WinterAdventurer.Library.Services;

namespace WinterAdventurer.Test
{
    /// <summary>
    /// Integration tests that verify the validation service correctly integrates with the UI logic
    /// These tests verify the validation state without rendering the full Blazor component
    /// </summary>
    [TestClass]
    public class HomeValidationIntegrationTests
    {
        private TimeslotValidationService _validationService = null!;

        [TestInitialize]
        public void Setup()
        {
            _validationService = new TimeslotValidationService();
        }

        #region Overlapping Timeslots Block PDF Generation

        [TestMethod]
        public void ValidationResult_WithOverlappingTimeslots_IndicatesPdfShouldBeBlocked()
        {
            // Arrange - Two timeslots with overlapping times
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Morning Period",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "2",
                    Label = "Late Morning",
                    StartTime = new TimeSpan(10, 0, 0), // Overlaps with first
                    EndTime = new TimeSpan(11, 30, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert - PDF generation should be blocked
            Assert.IsFalse(result.IsValid, "Should indicate validation failure");
            Assert.IsTrue(result.HasOverlappingTimeslots, "Should detect overlapping timeslots");

            // This state should disable PDF buttons in UI
            var shouldDisablePdfButtons = result.HasOverlappingTimeslots || result.HasUnconfiguredTimeslots;
            Assert.IsTrue(shouldDisablePdfButtons, "PDF buttons should be disabled");
        }

        [TestMethod]
        public void ValidationResult_WithIdenticalStartTimes_IndicatesPdfShouldBeBlocked()
        {
            // Arrange - Two custom timeslots with same start time (user's scenario)
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Late Night Activities",
                    StartTime = new TimeSpan(21, 15, 0),
                    EndTime = null, // Custom timeslot, end time optional
                    IsPeriod = false
                },
                new TimeSlotDto
                {
                    Id = "2",
                    Label = "Another Activity",
                    StartTime = new TimeSpan(21, 15, 0), // Same start time - should be caught!
                    EndTime = null,
                    IsPeriod = false
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid, "Duplicate start times should fail validation");
            Assert.IsTrue(result.HasOverlappingTimeslots, "Should detect duplicate start times as overlap");
            Assert.IsFalse(result.HasUnconfiguredTimeslots, "Custom timeslots can have null end times");
        }

        #endregion

        #region Unconfigured Period Timeslots Block PDF Generation

        [TestMethod]
        public void ValidationResult_WithUnconfiguredPeriodTimeslots_IndicatesPdfShouldBeBlocked()
        {
            // Arrange - Period timeslot missing end time
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Morning Period",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = null, // Period must have end time
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid, "Period timeslots must be configured");
            Assert.IsTrue(result.HasUnconfiguredTimeslots, "Should detect unconfigured period");

            var shouldDisablePdfButtons = result.HasOverlappingTimeslots || result.HasUnconfiguredTimeslots;
            Assert.IsTrue(shouldDisablePdfButtons, "PDF buttons should be disabled");
        }

        [TestMethod]
        public void ValidationResult_CustomTimeslotWithNoEndTime_DoesNotBlockPdf()
        {
            // Arrange - Custom timeslot (non-period) with no end time is allowed
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Morning Period",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "2",
                    Label = "Late Night Activity",
                    StartTime = new TimeSpan(21, 15, 0),
                    EndTime = null, // This is OK for custom timeslots
                    IsPeriod = false
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsTrue(result.IsValid, "Custom timeslots can have null end times");
            Assert.IsFalse(result.HasUnconfiguredTimeslots, "Should not flag custom timeslots");
            Assert.IsFalse(result.HasOverlappingTimeslots, "No overlaps present");
        }

        #endregion

        #region Valid Timeslots Allow PDF Generation

        [TestMethod]
        public void ValidationResult_WithValidTimeslots_AllowsPdfGeneration()
        {
            // Arrange - All period timeslots configured, no overlaps
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Morning Period",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "2",
                    Label = "Afternoon Period",
                    StartTime = new TimeSpan(13, 0, 0),
                    EndTime = new TimeSpan(15, 0, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsTrue(result.IsValid, "Valid timeslots should pass validation");
            Assert.IsFalse(result.HasOverlappingTimeslots, "No overlaps");
            Assert.IsFalse(result.HasUnconfiguredTimeslots, "All period timeslots configured");

            var shouldEnablePdfButtons = !result.HasOverlappingTimeslots && !result.HasUnconfiguredTimeslots;
            Assert.IsTrue(shouldEnablePdfButtons, "PDF buttons should be enabled");
        }

        #endregion

        #region Real-Time Validation Scenarios

        [TestMethod]
        public void ValidationResult_AfterUserChangesTime_RevalidatesCorrectly()
        {
            // Arrange - Initial valid state
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Period 1",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 0, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "2",
                    Label = "Period 2",
                    StartTime = new TimeSpan(11, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    IsPeriod = true
                }
            };

            var result = _validationService.ValidateTimeslots(timeslots);
            Assert.IsTrue(result.IsValid, "Initial state should be valid");

            // Act - User changes second period to overlap
            timeslots[1].StartTime = new TimeSpan(9, 30, 0); // Now overlaps with first period
            result = _validationService.ValidateTimeslots(timeslots);

            // Assert - Validation should now fail
            Assert.IsFalse(result.IsValid, "Should detect overlap after time change");
            Assert.IsTrue(result.HasOverlappingTimeslots, "Should flag overlapping timeslots");
        }

        [TestMethod]
        public void ValidationResult_AfterUserFixesOverlap_BecomesValid()
        {
            // Arrange - Start with overlapping timeslots
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Period 1",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "2",
                    Label = "Period 2",
                    StartTime = new TimeSpan(10, 0, 0), // Overlaps
                    EndTime = new TimeSpan(11, 30, 0),
                    IsPeriod = true
                }
            };

            var result = _validationService.ValidateTimeslots(timeslots);
            Assert.IsFalse(result.IsValid, "Should start invalid due to overlap");

            // Act - User fixes the overlap
            timeslots[1].StartTime = new TimeSpan(10, 30, 0); // No longer overlaps
            result = _validationService.ValidateTimeslots(timeslots);

            // Assert - Should now be valid
            Assert.IsTrue(result.IsValid, "Should become valid after fixing overlap");
            Assert.IsFalse(result.HasOverlappingTimeslots, "No overlap after fix");
        }

        #endregion

        #region Error Message Priority

        [TestMethod]
        public void ValidationResult_WithBothErrors_IndicatesBoth()
        {
            // Arrange - Both overlapping AND unconfigured
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Period 1",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "2",
                    Label = "Period 2",
                    StartTime = new TimeSpan(10, 0, 0), // Overlaps
                    EndTime = new TimeSpan(11, 30, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "3",
                    Label = "Period 3",
                    StartTime = null, // Unconfigured
                    EndTime = new TimeSpan(15, 0, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert - Both errors should be flagged
            Assert.IsFalse(result.IsValid, "Should be invalid");
            Assert.IsTrue(result.HasOverlappingTimeslots, "Should detect overlaps");
            Assert.IsTrue(result.HasUnconfiguredTimeslots, "Should detect unconfigured");

            // UI should prioritize unconfigured error (based on if-else structure in Home.razor)
            // But PDF buttons should be disabled regardless
            var shouldDisablePdfButtons = result.HasOverlappingTimeslots || result.HasUnconfiguredTimeslots;
            Assert.IsTrue(shouldDisablePdfButtons, "PDF buttons disabled for either error");
        }

        #endregion
    }
}
