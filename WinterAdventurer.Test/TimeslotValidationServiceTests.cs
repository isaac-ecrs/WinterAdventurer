using WinterAdventurer.Library.Services;

namespace WinterAdventurer.Test
{
    [TestClass]
    public class TimeslotValidationServiceTests
    {
        private ITimeslotValidationService _validationService = null!;

        [TestInitialize]
        public void Setup()
        {
            _validationService = new TimeslotValidationService();
        }

        #region No Issues - Valid Scenarios

        [TestMethod]
        public void ValidateTimeslots_NoTimeslots_ReturnsValid()
        {
            // Arrange
            var timeslots = new List<TimeSlotDto>();

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_SingleTimeslotWithTimes_ReturnsValid()
        {
            // Arrange
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Morning Period",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_NonOverlappingTimeslots_ReturnsValid()
        {
            // Arrange
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
                    Label = "Lunch",
                    StartTime = new TimeSpan(12, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    IsPeriod = false
                },
                new TimeSlotDto
                {
                    Id = "3",
                    Label = "Afternoon Period",
                    StartTime = new TimeSpan(13, 30, 0),
                    EndTime = new TimeSpan(15, 0, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_AdjacentTimeslots_ReturnsValid()
        {
            // Arrange - timeslots that end exactly when the next starts (no gap, no overlap)
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
                    StartTime = new TimeSpan(10, 30, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_NonPeriodTimeslotsWithoutTimes_ReturnsValid()
        {
            // Arrange - Non-period timeslots can be unconfigured
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
                    Label = "Custom Activity",
                    StartTime = null,
                    EndTime = null,
                    IsPeriod = false // Not a period, so it's okay to be unconfigured
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        #endregion

        #region Unconfigured Timeslots Detection

        [TestMethod]
        public void ValidateTimeslots_PeriodMissingStartTime_DetectsUnconfigured()
        {
            // Arrange
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Morning Period",
                    StartTime = null,
                    EndTime = new TimeSpan(10, 30, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.HasUnconfiguredTimeslots);
            Assert.IsFalse(result.HasOverlappingTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_PeriodMissingEndTime_DetectsUnconfigured()
        {
            // Arrange
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Morning Period",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = null,
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.HasUnconfiguredTimeslots);
            Assert.IsFalse(result.HasOverlappingTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_PeriodMissingBothTimes_DetectsUnconfigured()
        {
            // Arrange
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Morning Period",
                    StartTime = null,
                    EndTime = null,
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.HasUnconfiguredTimeslots);
            Assert.IsFalse(result.HasOverlappingTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_MultiplePeriods_OneUnconfigured_DetectsUnconfigured()
        {
            // Arrange
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
                    StartTime = null,
                    EndTime = new TimeSpan(15, 0, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.HasUnconfiguredTimeslots);
            Assert.IsFalse(result.HasOverlappingTimeslots);
        }

        #endregion

        #region Overlapping Timeslots Detection

        [TestMethod]
        public void ValidateTimeslots_OverlappingTimeslots_DetectsOverlap()
        {
            // Arrange - second period starts before first ends
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
                    StartTime = new TimeSpan(10, 0, 0), // Starts 30 min before first ends
                    EndTime = new TimeSpan(11, 30, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_CompletelyOverlappingTimeslots_DetectsOverlap()
        {
            // Arrange - one timeslot completely contains another
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "All Day",
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "2",
                    Label = "Lunch",
                    StartTime = new TimeSpan(12, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    IsPeriod = false
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_MinimalOverlap_DetectsOverlap()
        {
            // Arrange - overlap by just 1 minute
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
                    StartTime = new TimeSpan(10, 29, 0), // 1 minute before first ends
                    EndTime = new TimeSpan(11, 30, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_UnsortedOverlappingTimeslots_DetectsOverlap()
        {
            // Arrange - timeslots provided in non-chronological order
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "2",
                    Label = "Afternoon",
                    StartTime = new TimeSpan(13, 0, 0),
                    EndTime = new TimeSpan(15, 0, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Late Morning",
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(13, 30, 0), // Overlaps with afternoon
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        #endregion

        #region Both Issues Present

        [TestMethod]
        public void ValidateTimeslots_BothOverlapAndUnconfigured_DetectsBoth()
        {
            // Arrange
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
                    StartTime = new TimeSpan(10, 0, 0), // Overlaps
                    EndTime = new TimeSpan(11, 30, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "3",
                    Label = "Afternoon Period",
                    StartTime = null, // Unconfigured
                    EndTime = new TimeSpan(15, 0, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.HasOverlappingTimeslots);
            Assert.IsTrue(result.HasUnconfiguredTimeslots);
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void ValidateTimeslots_MultipleTimeslotsWithNullTimes_IgnoresNullsForOverlapCheck()
        {
            // Arrange - timeslots with null times shouldn't cause overlaps
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "TBD Period 1",
                    StartTime = null,
                    EndTime = null,
                    IsPeriod = false
                },
                new TimeSlotDto
                {
                    Id = "2",
                    Label = "TBD Period 2",
                    StartTime = null,
                    EndTime = null,
                    IsPeriod = false
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots); // Not periods, so OK
        }

        [TestMethod]
        public void ValidateTimeslots_ThreeConsecutiveTimeslots_ReturnsValid()
        {
            // Arrange
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
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(11, 0, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "3",
                    Label = "Period 3",
                    StartTime = new TimeSpan(11, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_MultiDaySchedule_HandlesCorrectly()
        {
            // Arrange - realistic multi-day schedule
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
                    Label = "Break",
                    StartTime = new TimeSpan(10, 30, 0),
                    EndTime = new TimeSpan(10, 45, 0),
                    IsPeriod = false
                },
                new TimeSlotDto
                {
                    Id = "3",
                    Label = "Late Morning Period",
                    StartTime = new TimeSpan(10, 45, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "4",
                    Label = "Lunch",
                    StartTime = new TimeSpan(12, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    IsPeriod = false
                },
                new TimeSlotDto
                {
                    Id = "5",
                    Label = "Afternoon Period",
                    StartTime = new TimeSpan(13, 0, 0),
                    EndTime = new TimeSpan(15, 0, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_SameStartAndEndTime_IsValid()
        {
            // Arrange - zero-duration timeslot (edge case)
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Instantaneous Event",
                    StartTime = new TimeSpan(12, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_IdenticalTimeslots_DetectsOverlap()
        {
            // Arrange - Two timeslots with exact same start and end times
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
                    Label = "Another Morning Period",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid, "Identical timeslots should be detected as overlapping");
            Assert.IsTrue(result.HasOverlappingTimeslots, "Should detect overlap when two timeslots have identical times");
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_TwoCustomTimeslotsWithSameStartNoEnd_DetectsOverlap()
        {
            // Arrange - User scenario: two custom timeslots with same start, no end time
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Late Night Activities",
                    StartTime = new TimeSpan(21, 15, 0), // 9:15 PM
                    EndTime = null,
                    IsPeriod = false
                },
                new TimeSlotDto
                {
                    Id = "2",
                    Label = "Another Late Night Activity",
                    StartTime = new TimeSpan(21, 15, 0), // 9:15 PM - same start time
                    EndTime = null,
                    IsPeriod = false
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid, "Two timeslots with same start time should be detected as overlapping");
            Assert.IsTrue(result.HasOverlappingTimeslots, "Should detect overlap when start times are identical");
            Assert.IsFalse(result.HasUnconfiguredTimeslots, "Custom timeslots can have null end times");
        }

        #endregion

        #region Real-World Scenarios

        [TestMethod]
        public void ValidateTimeslots_TypicalWinterAdventureSchedule_ReturnsValid()
        {
            // Arrange - Typical Winter Adventure daily schedule
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Morning First Period",
                    StartTime = new TimeSpan(9, 15, 0),
                    EndTime = new TimeSpan(10, 45, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "2",
                    Label = "Morning Second Period",
                    StartTime = new TimeSpan(11, 0, 0),
                    EndTime = new TimeSpan(12, 30, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "3",
                    Label = "Lunch",
                    StartTime = new TimeSpan(12, 30, 0),
                    EndTime = new TimeSpan(13, 30, 0),
                    IsPeriod = false
                },
                new TimeSlotDto
                {
                    Id = "4",
                    Label = "Afternoon First Period",
                    StartTime = new TimeSpan(13, 30, 0),
                    EndTime = new TimeSpan(15, 0, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "5",
                    Label = "Afternoon Second Period",
                    StartTime = new TimeSpan(15, 15, 0),
                    EndTime = new TimeSpan(16, 45, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_UserForgetsToConfigureOnePeriod_DetectsError()
        {
            // Arrange - Realistic scenario where user uploaded Excel but forgot to set times for one period
            var timeslots = new List<TimeSlotDto>
            {
                new TimeSlotDto
                {
                    Id = "1",
                    Label = "Morning First Period",
                    StartTime = new TimeSpan(9, 15, 0),
                    EndTime = new TimeSpan(10, 45, 0),
                    IsPeriod = true
                },
                new TimeSlotDto
                {
                    Id = "2",
                    Label = "Morning Second Period",
                    StartTime = null, // User forgot to configure this
                    EndTime = null,
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsFalse(result.HasOverlappingTimeslots);
            Assert.IsTrue(result.HasUnconfiguredTimeslots);
        }

        [TestMethod]
        public void ValidateTimeslots_UserAccidentallyCreatesOverlap_DetectsError()
        {
            // Arrange - Realistic scenario where user makes a typo in time entry
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
                    Label = "Late Morning Period",
                    StartTime = new TimeSpan(10, 0, 0), // Typo - meant 11:00
                    EndTime = new TimeSpan(11, 30, 0),
                    IsPeriod = true
                }
            };

            // Act
            var result = _validationService.ValidateTimeslots(timeslots);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.HasOverlappingTimeslots);
            Assert.IsFalse(result.HasUnconfiguredTimeslots);
        }

        #endregion
    }
}
