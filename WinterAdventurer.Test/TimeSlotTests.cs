// <copyright file="TimeSlotTests.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using WinterAdventurer.Library.Models;

namespace WinterAdventurer.Test
{
    [TestClass]
    public class TimeSlotTests
    {
        [TestMethod]
        public void TimeRange_WithBothStartAndEndTime_ReturnsFormattedRange()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                StartTime = new TimeSpan(9, 0, 0),   // 9:00 AM
                EndTime = new TimeSpan(10, 30, 0),    // 10:30 AM
            };

            // Act
            var result = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual("9:00 AM - 10:30 AM", result);
        }

        [TestMethod]
        public void TimeRange_WithAfternoonTimes_UsesCorrectPMFormat()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                StartTime = new TimeSpan(14, 15, 0),  // 2:15 PM
                EndTime = new TimeSpan(16, 45, 0),     // 4:45 PM
            };

            // Act
            var result = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual("2:15 PM - 4:45 PM", result);
        }

        [TestMethod]
        public void TimeRange_WithMidnightTime_FormatsCorrectly()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                StartTime = new TimeSpan(0, 0, 0),   // Midnight
                EndTime = new TimeSpan(1, 0, 0),      // 1:00 AM
            };

            // Act
            var result = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual("12:00 AM - 1:00 AM", result);
        }

        [TestMethod]
        public void TimeRange_WithNoonTime_FormatsCorrectly()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                StartTime = new TimeSpan(12, 0, 0),   // Noon
                EndTime = new TimeSpan(13, 30, 0),     // 1:30 PM
            };

            // Act
            var result = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual("12:00 PM - 1:30 PM", result);
        }

        [TestMethod]
        public void TimeRange_SpanningAMtoPM_FormatsCorrectly()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                StartTime = new TimeSpan(11, 30, 0),  // 11:30 AM
                EndTime = new TimeSpan(13, 0, 0),      // 1:00 PM
            };

            // Act
            var result = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual("11:30 AM - 1:00 PM", result);
        }

        [TestMethod]
        public void TimeRange_WithOnlyStartTime_ReturnsOpenEndedFormat()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                StartTime = new TimeSpan(21, 0, 0),  // 9:00 PM
                EndTime = null,
            };

            // Act
            var result = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual("9:00 PM - ?", result);
        }

        [TestMethod]
        public void TimeRange_WithNoTimes_ReturnsEmptyString()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                StartTime = null,
                EndTime = null,
            };

            // Act
            var result = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void TimeRange_WithOnlyEndTime_ReturnsEmptyString()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                StartTime = null,
                EndTime = new TimeSpan(10, 0, 0),
            };

            // Act
            var result = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void TimeRange_WithEarlyMorningTimes_FormatsCorrectly()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                StartTime = new TimeSpan(6, 30, 0),   // 6:30 AM
                EndTime = new TimeSpan(8, 0, 0),       // 8:00 AM
            };

            // Act
            var result = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual("6:30 AM - 8:00 AM", result);
        }

        [TestMethod]
        public void TimeRange_WithLateEveningTimes_FormatsCorrectly()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                StartTime = new TimeSpan(20, 0, 0),   // 8:00 PM
                EndTime = new TimeSpan(22, 30, 0),     // 10:30 PM
            };

            // Act
            var result = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual("8:00 PM - 10:30 PM", result);
        }

        [TestMethod]
        public void TimeRange_WithExactHourTimes_FormatsWithZeroMinutes()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                StartTime = new TimeSpan(10, 0, 0),   // 10:00 AM
                EndTime = new TimeSpan(11, 0, 0),      // 11:00 AM
            };

            // Act
            var result = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual("10:00 AM - 11:00 AM", result);
        }

        [TestMethod]
        public void TimeSlot_DefaultConstructor_SetsDefaultValues()
        {
            // Act
            var timeSlot = new TimeSlot();

            // Assert
            Assert.IsNotNull(timeSlot.Id);
            Assert.AreNotEqual(Guid.Empty.ToString(), timeSlot.Id);
            Assert.AreEqual(string.Empty, timeSlot.Label);
            Assert.IsNull(timeSlot.StartTime);
            Assert.IsNull(timeSlot.EndTime);
            Assert.IsFalse(timeSlot.IsPeriod);
        }

        [TestMethod]
        public void TimeSlot_Id_GeneratesUniqueValues()
        {
            // Act
            var timeSlot1 = new TimeSlot();
            var timeSlot2 = new TimeSlot();

            // Assert
            Assert.AreNotEqual(timeSlot1.Id, timeSlot2.Id);
        }

        [TestMethod]
        public void TimeSlot_Properties_CanBeSet()
        {
            // Arrange
            var expectedId = "custom-id";
            var expectedLabel = "Morning Session";
            var expectedStartTime = new TimeSpan(9, 0, 0);
            var expectedEndTime = new TimeSpan(12, 0, 0);
            var expectedIsPeriod = true;

            // Act
            var timeSlot = new TimeSlot
            {
                Id = expectedId,
                Label = expectedLabel,
                StartTime = expectedStartTime,
                EndTime = expectedEndTime,
                IsPeriod = expectedIsPeriod,
            };

            // Assert
            Assert.AreEqual(expectedId, timeSlot.Id);
            Assert.AreEqual(expectedLabel, timeSlot.Label);
            Assert.AreEqual(expectedStartTime, timeSlot.StartTime);
            Assert.AreEqual(expectedEndTime, timeSlot.EndTime);
            Assert.AreEqual(expectedIsPeriod, timeSlot.IsPeriod);
        }
    }
}
