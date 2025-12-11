// <copyright file="ModelTests.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using WinterAdventurer.Library.Models;

namespace WinterAdventurer.Test
{
    [TestClass]
    public class ModelTests
    {
        [TestMethod]
        public void WorkshopDuration_SingleDay_CalculatesCorrectly()
        {
            // Arrange & Act
            var duration = new WorkshopDuration(1, 1);

            // Assert
            Assert.AreEqual(1, duration.StartDay);
            Assert.AreEqual(1, duration.EndDay);
            Assert.AreEqual(1, duration.NumberOfDays);
            Assert.AreEqual("Day 1", duration.Description);
        }

        [TestMethod]
        public void WorkshopDuration_FourDays_CalculatesCorrectly()
        {
            // Arrange & Act
            var duration = new WorkshopDuration(1, 4);

            // Assert
            Assert.AreEqual(1, duration.StartDay);
            Assert.AreEqual(4, duration.EndDay);
            Assert.AreEqual(4, duration.NumberOfDays);
            Assert.AreEqual("Days 1-4", duration.Description);
        }

        [TestMethod]
        public void WorkshopDuration_TwoDays_CalculatesCorrectly()
        {
            // Arrange & Act
            var duration = new WorkshopDuration(1, 2);

            // Assert
            Assert.AreEqual(2, duration.NumberOfDays);
            Assert.AreEqual("Days 1-2", duration.Description);
        }

        [TestMethod]
        public void WorkshopDuration_SecondHalfOfWeek_CalculatesCorrectly()
        {
            // Arrange & Act
            var duration = new WorkshopDuration(3, 4);

            // Assert
            Assert.AreEqual(3, duration.StartDay);
            Assert.AreEqual(4, duration.EndDay);
            Assert.AreEqual(2, duration.NumberOfDays);
            Assert.AreEqual("Days 3-4", duration.Description);
        }

        [TestMethod]
        public void WorkshopDuration_LastDay_CalculatesCorrectly()
        {
            // Arrange & Act
            var duration = new WorkshopDuration(4, 4);

            // Assert
            Assert.AreEqual(1, duration.NumberOfDays);
            Assert.AreEqual("Day 4", duration.Description);
        }

        [TestMethod]
        public void WorkshopDuration_MiddleDays_CalculatesCorrectly()
        {
            // Arrange & Act
            var duration = new WorkshopDuration(2, 3);

            // Assert
            Assert.AreEqual(2, duration.NumberOfDays);
            Assert.AreEqual("Days 2-3", duration.Description);
        }

        [TestMethod]
        public void Period_SimpleSheetName_SetsDisplayNameCorrectly()
        {
            // Arrange & Act
            var period = new Period("Morning");

            // Assert
            Assert.AreEqual("Morning", period.SheetName);
            Assert.AreEqual("Morning", period.DisplayName);
        }

        [TestMethod]
        public void Period_CamelCaseSheetName_InsertsSpaces()
        {
            // Arrange & Act
            var period = new Period("MorningFirstPeriod");

            // Assert
            Assert.AreEqual("MorningFirstPeriod", period.SheetName);
            Assert.AreEqual("Morning First Period", period.DisplayName);
        }

        [TestMethod]
        public void Period_MultipleCapitalLetters_InsertsSpacesProperly()
        {
            // Arrange & Act
            var period = new Period("AfternoonSecondPeriod");

            // Assert
            Assert.AreEqual("AfternoonSecondPeriod", period.SheetName);
            Assert.AreEqual("Afternoon Second Period", period.DisplayName);
        }

        [TestMethod]
        public void Period_AllLowercase_NoSpacesAdded()
        {
            // Arrange & Act
            var period = new Period("afternoon");

            // Assert
            Assert.AreEqual("afternoon", period.SheetName);
            Assert.AreEqual("afternoon", period.DisplayName);
        }

        [TestMethod]
        public void Period_SingleCapitalLetter_NoSpaceAdded()
        {
            // Arrange & Act
            var period = new Period("A");

            // Assert
            Assert.AreEqual("A", period.SheetName);
            Assert.AreEqual("A", period.DisplayName);
        }

        [TestMethod]
        public void Period_ConsecutiveCapitalLetters_InsertsSpacesCorrectly()
        {
            // Arrange & Act
            var period = new Period("MorningAMSession");

            // Assert
            Assert.AreEqual("MorningAMSession", period.SheetName);
            Assert.AreEqual("Morning A M Session", period.DisplayName);
        }

        [TestMethod]
        public void Workshop_Key_GeneratesUniqueIdentifier()
        {
            // Arrange
            var period = new Period("MorningFirstPeriod");
            var duration = new WorkshopDuration(1, 4);
            var workshop = new Workshop
            {
                Name = "Pottery",
                Leader = "John Smith",
                Period = period,
                Duration = duration,
            };

            // Act
            string key = workshop.Key;

            // Assert
            Assert.AreEqual("MorningFirstPeriod|Pottery|John Smith|1-4", key);
        }

        [TestMethod]
        public void Workshop_Key_DifferentLeaders_GeneratesDifferentKeys()
        {
            // Arrange
            var period = new Period("MorningFirstPeriod");
            var duration = new WorkshopDuration(1, 4);

            var workshop1 = new Workshop
            {
                Name = "Pottery",
                Leader = "John Smith",
                Period = period,
                Duration = duration,
            };

            var workshop2 = new Workshop
            {
                Name = "Pottery",
                Leader = "Jane Doe",
                Period = period,
                Duration = duration,
            };

            // Act
            string key1 = workshop1.Key;
            string key2 = workshop2.Key;

            // Assert
            Assert.AreNotEqual(key1, key2);
            Assert.AreEqual("MorningFirstPeriod|Pottery|John Smith|1-4", key1);
            Assert.AreEqual("MorningFirstPeriod|Pottery|Jane Doe|1-4", key2);
        }

        [TestMethod]
        public void Workshop_Key_DifferentDurations_GeneratesDifferentKeys()
        {
            // Arrange
            var period = new Period("MorningFirstPeriod");
            var duration1 = new WorkshopDuration(1, 4);
            var duration2 = new WorkshopDuration(1, 2);

            var workshop1 = new Workshop
            {
                Name = "Pottery",
                Leader = "John Smith",
                Period = period,
                Duration = duration1,
            };

            var workshop2 = new Workshop
            {
                Name = "Pottery",
                Leader = "John Smith",
                Period = period,
                Duration = duration2,
            };

            // Act
            string key1 = workshop1.Key;
            string key2 = workshop2.Key;

            // Assert
            Assert.AreNotEqual(key1, key2);
            Assert.AreEqual("MorningFirstPeriod|Pottery|John Smith|1-4", key1);
            Assert.AreEqual("MorningFirstPeriod|Pottery|John Smith|1-2", key2);
        }

        [TestMethod]
        public void Workshop_Key_DifferentPeriods_GeneratesDifferentKeys()
        {
            // Arrange
            var period1 = new Period("MorningFirstPeriod");
            var period2 = new Period("AfternoonSecondPeriod");
            var duration = new WorkshopDuration(1, 4);

            var workshop1 = new Workshop
            {
                Name = "Pottery",
                Leader = "John Smith",
                Period = period1,
                Duration = duration,
            };

            var workshop2 = new Workshop
            {
                Name = "Pottery",
                Leader = "John Smith",
                Period = period2,
                Duration = duration,
            };

            // Act
            string key1 = workshop1.Key;
            string key2 = workshop2.Key;

            // Assert
            Assert.AreNotEqual(key1, key2);
            Assert.AreEqual("MorningFirstPeriod|Pottery|John Smith|1-4", key1);
            Assert.AreEqual("AfternoonSecondPeriod|Pottery|John Smith|1-4", key2);
        }

        [TestMethod]
        public void Workshop_DefaultValues_InitializedCorrectly()
        {
            // Arrange & Act
            var workshop = new Workshop();

            // Assert
            Assert.AreEqual(string.Empty, workshop.Name);
            Assert.AreEqual(string.Empty, workshop.Location);
            Assert.AreEqual(string.Empty, workshop.Leader);
            Assert.IsFalse(workshop.IsMini);
            Assert.AreEqual(0, workshop.MaxParticipants);
            Assert.AreEqual(0, workshop.MinAge);
            Assert.IsNotNull(workshop.Selections);
            Assert.AreEqual(0, workshop.Selections.Count);
            Assert.IsNotNull(workshop.Duration);
        }

        [TestMethod]
        public void Workshop_Selections_CanAddAndRetrieve()
        {
            // Arrange
            var workshop = new Workshop
            {
                Name = "Pottery",
                Leader = "John Smith",
                Period = new Period("Morning"),
                Duration = new WorkshopDuration(1, 4),
            };

            var selection1 = new WorkshopSelection { ChoiceNumber = 1 };
            var selection2 = new WorkshopSelection { ChoiceNumber = 2 };

            // Act
            workshop.Selections.Add(selection1);
            workshop.Selections.Add(selection2);

            // Assert
            Assert.AreEqual(2, workshop.Selections.Count);
            Assert.AreEqual(1, workshop.Selections[0].ChoiceNumber);
            Assert.AreEqual(2, workshop.Selections[1].ChoiceNumber);
        }

        [TestMethod]
        public void Workshop_CompleteScenario_AllPropertiesWork()
        {
            // Arrange
            var period = new Period("MorningFirstPeriod");
            var duration = new WorkshopDuration(1, 4);
            var workshop = new Workshop
            {
                Name = "Advanced Pottery",
                Leader = "John Smith",
                Location = "Art Studio",
                Period = period,
                Duration = duration,
                IsMini = false,
                MaxParticipants = 15,
                MinAge = 12,
            };

            var selection = new WorkshopSelection
            {
                ChoiceNumber = 1,
                ClassSelectionId = "12345",
            };
            workshop.Selections.Add(selection);

            // Act & Assert
            Assert.AreEqual("Advanced Pottery", workshop.Name);
            Assert.AreEqual("John Smith", workshop.Leader);
            Assert.AreEqual("Art Studio", workshop.Location);
            Assert.AreEqual("MorningFirstPeriod|Advanced Pottery|John Smith|1-4", workshop.Key);
            Assert.AreEqual("Morning First Period", workshop.Period.DisplayName);
            Assert.AreEqual(4, workshop.Duration.NumberOfDays);
            Assert.AreEqual("Days 1-4", workshop.Duration.Description);
            Assert.AreEqual(15, workshop.MaxParticipants);
            Assert.AreEqual(12, workshop.MinAge);
            Assert.AreEqual(1, workshop.Selections.Count);
        }

        [TestMethod]
        public void Period_DisplayName_RealWorldExamples()
        {
            // Test various real-world period names
            var testCases = new[]
            {
                new { SheetName = "MorningFirstPeriod", Expected = "Morning First Period" },
                new { SheetName = "AfternoonSecondPeriod", Expected = "Afternoon Second Period" },
                new { SheetName = "EveningSession", Expected = "Evening Session" },
                new { SheetName = "AllDay", Expected = "All Day" },
                new { SheetName = "LunchBreak", Expected = "Lunch Break" },
            };

            foreach (var testCase in testCases)
            {
                var period = new Period(testCase.SheetName);
                Assert.AreEqual(testCase.Expected, period.DisplayName,
                    $"Failed for SheetName: {testCase.SheetName}");
            }
        }

        [TestMethod]
        public void WorkshopDuration_VariousScenarios_CalculateCorrectly()
        {
            // Test various duration scenarios
            var testCases = new[]
            {
                new { Start = 1, End = 1, Days = 1, Desc = "Day 1" },
                new { Start = 1, End = 2, Days = 2, Desc = "Days 1-2" },
                new { Start = 1, End = 4, Days = 4, Desc = "Days 1-4" },
                new { Start = 3, End = 4, Days = 2, Desc = "Days 3-4" },
                new { Start = 2, End = 2, Days = 1, Desc = "Day 2" },
            };

            foreach (var testCase in testCases)
            {
                var duration = new WorkshopDuration(testCase.Start, testCase.End);
                Assert.AreEqual(testCase.Days, duration.NumberOfDays,
                    $"Failed NumberOfDays for {testCase.Start}-{testCase.End}");
                Assert.AreEqual(testCase.Desc, duration.Description,
                    $"Failed Description for {testCase.Start}-{testCase.End}");
            }
        }

        [TestMethod]
        public void TimeSlot_DefaultValues_InitializedCorrectly()
        {
            // Arrange & Act
            var timeSlot = new TimeSlot();

            // Assert
            Assert.IsNotNull(timeSlot.Id);
            Assert.IsFalse(string.IsNullOrEmpty(timeSlot.Id));
            Assert.AreEqual(string.Empty, timeSlot.Label);
            Assert.IsNull(timeSlot.StartTime);
            Assert.IsNull(timeSlot.EndTime);
            Assert.IsFalse(timeSlot.IsPeriod);
            Assert.AreEqual(string.Empty, timeSlot.TimeRange);
        }

        [TestMethod]
        public void TimeSlot_WithStartAndEndTime_FormatsTimeRangeCorrectly()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                Label = "Morning Session",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(9, 30, 0),
                IsPeriod = true,
            };

            // Act
            var timeRange = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual("8:00 AM - 9:30 AM", timeRange);
        }

        [TestMethod]
        public void TimeSlot_WithOnlyStartTime_FormatsWithQuestionMark()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                Label = "Free Time",
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = null,
            };

            // Act
            var timeRange = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual("2:00 PM - ?", timeRange);
        }

        [TestMethod]
        public void TimeSlot_WithNoTimes_ReturnsEmptyTimeRange()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                Label = "TBD",
                StartTime = null,
                EndTime = null,
            };

            // Act
            var timeRange = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual(string.Empty, timeRange);
        }

        [TestMethod]
        public void TimeSlot_IsPeriod_CanBeSetAndRetrieved()
        {
            // Arrange
            var periodSlot = new TimeSlot { IsPeriod = true };
            var activitySlot = new TimeSlot { IsPeriod = false };

            // Assert
            Assert.IsTrue(periodSlot.IsPeriod);
            Assert.IsFalse(activitySlot.IsPeriod);
        }

        [TestMethod]
        public void TimeSlot_AfternoonTime_FormatsCorrectly()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                StartTime = new TimeSpan(13, 30, 0),
                EndTime = new TimeSpan(15, 0, 0),
            };

            // Act
            var timeRange = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual("1:30 PM - 3:00 PM", timeRange);
        }

        [TestMethod]
        public void TimeSlot_MidnightTime_FormatsCorrectly()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                StartTime = new TimeSpan(0, 0, 0),
                EndTime = new TimeSpan(1, 0, 0),
            };

            // Act
            var timeRange = timeSlot.TimeRange;

            // Assert
            Assert.AreEqual("12:00 AM - 1:00 AM", timeRange);
        }

        [TestMethod]
        public void WorkshopSelection_DefaultValues_InitializedCorrectly()
        {
            // Arrange & Act
            var selection = new WorkshopSelection();

            // Assert
            Assert.AreEqual(string.Empty, selection.ClassSelectionId);
            Assert.AreEqual(string.Empty, selection.WorkshopName);
            Assert.AreEqual(string.Empty, selection.FullName);
            Assert.AreEqual(string.Empty, selection.FirstName);
            Assert.AreEqual(string.Empty, selection.LastName);
            Assert.AreEqual(0, selection.ChoiceNumber);
            Assert.AreEqual(0, selection.RegistrationId);
            Assert.IsNotNull(selection.Duration);
        }

        [TestMethod]
        public void WorkshopSelection_AllProperties_CanBeSetAndRetrieved()
        {
            // Arrange
            var duration = new WorkshopDuration(1, 4);
            var selection = new WorkshopSelection
            {
                ClassSelectionId = "12345",
                WorkshopName = "Pottery",
                FullName = "John Smith",
                FirstName = "John",
                LastName = "Smith",
                ChoiceNumber = 1,
                RegistrationId = 100,
                Duration = duration,
            };

            // Assert
            Assert.AreEqual("12345", selection.ClassSelectionId);
            Assert.AreEqual("Pottery", selection.WorkshopName);
            Assert.AreEqual("John Smith", selection.FullName);
            Assert.AreEqual("John", selection.FirstName);
            Assert.AreEqual("Smith", selection.LastName);
            Assert.AreEqual(1, selection.ChoiceNumber);
            Assert.AreEqual(100, selection.RegistrationId);
            Assert.AreEqual(duration, selection.Duration);
        }

        [TestMethod]
        public void WorkshopSelection_FirstChoice_HasChoiceNumberOne()
        {
            // Arrange & Act
            var selection = new WorkshopSelection
            {
                ChoiceNumber = 1,
            };

            // Assert
            Assert.AreEqual(1, selection.ChoiceNumber);
        }

        [TestMethod]
        public void WorkshopSelection_BackupChoice_HasChoiceNumberGreaterThanOne()
        {
            // Arrange & Act
            var selection = new WorkshopSelection
            {
                ChoiceNumber = 2,
            };

            // Assert
            Assert.IsTrue(selection.ChoiceNumber > 1);
            Assert.AreEqual(2, selection.ChoiceNumber);
        }

        [TestMethod]
        public void WorkshopSelection_ToString_ReturnsJsonRepresentation()
        {
            // Arrange
            var selection = new WorkshopSelection
            {
                ClassSelectionId = "12345",
                WorkshopName = "Pottery",
                FirstName = "John",
                LastName = "Smith",
                ChoiceNumber = 1,
            };

            // Act
            var json = selection.ToString();

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(json));
            Assert.IsTrue(json.Contains("12345"));
            Assert.IsTrue(json.Contains("Pottery"));
            Assert.IsTrue(json.Contains("John"));
            Assert.IsTrue(json.Contains("Smith"));
        }

        [TestMethod]
        public void WorkshopSelection_WithDifferentDurations_MaintainsDuration()
        {
            // Arrange
            var duration12 = new WorkshopDuration(1, 2);
            var duration34 = new WorkshopDuration(3, 4);

            var selection1 = new WorkshopSelection { Duration = duration12 };
            var selection2 = new WorkshopSelection { Duration = duration34 };

            // Assert
            Assert.AreEqual(2, selection1.Duration.NumberOfDays);
            Assert.AreEqual(2, selection2.Duration.NumberOfDays);
            Assert.AreEqual("Days 1-2", selection1.Duration.Description);
            Assert.AreEqual("Days 3-4", selection2.Duration.Description);
        }

        [TestMethod]
        public void Attendee_DefaultValues_InitializedCorrectly()
        {
            // Arrange & Act
            var attendee = new Attendee();

            // Assert
            Assert.AreEqual(string.Empty, attendee.ClassSelectionId);
            Assert.AreEqual(string.Empty, attendee.FirstName);
            Assert.AreEqual(string.Empty, attendee.LastName);
            Assert.AreEqual(string.Empty, attendee.Email);
            Assert.AreEqual(string.Empty, attendee.Age);
            Assert.AreEqual(" ", attendee.FullName); // FirstName + space + LastName
        }

        [TestMethod]
        public void Attendee_AllProperties_CanBeSetAndRetrieved()
        {
            // Arrange
            var attendee = new Attendee
            {
                ClassSelectionId = "12345",
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@example.com",
                Age = "14",
            };

            // Assert
            Assert.AreEqual("12345", attendee.ClassSelectionId);
            Assert.AreEqual("John", attendee.FirstName);
            Assert.AreEqual("Smith", attendee.LastName);
            Assert.AreEqual("john.smith@example.com", attendee.Email);
            Assert.AreEqual("14", attendee.Age);
        }

        [TestMethod]
        public void Attendee_FullName_CombinesFirstAndLastName()
        {
            // Arrange
            var attendee = new Attendee
            {
                FirstName = "Jane",
                LastName = "Doe",
            };

            // Act
            var fullName = attendee.FullName;

            // Assert
            Assert.AreEqual("Jane Doe", fullName);
        }

        [TestMethod]
        public void Attendee_FullName_WithOnlyFirstName_ShowsFirstNameWithSpace()
        {
            // Arrange
            var attendee = new Attendee
            {
                FirstName = "Alice",
                LastName = string.Empty,
            };

            // Act
            var fullName = attendee.FullName;

            // Assert
            Assert.AreEqual("Alice ", fullName);
        }

        [TestMethod]
        public void Attendee_AgeRange_CanBeStored()
        {
            // Arrange
            var attendee = new Attendee
            {
                Age = "12-14",
            };

            // Assert
            Assert.AreEqual("12-14", attendee.Age);
        }

        [TestMethod]
        public void Attendee_ToString_ReturnsJsonRepresentation()
        {
            // Arrange
            var attendee = new Attendee
            {
                ClassSelectionId = "12345",
                FirstName = "John",
                LastName = "Smith",
                Email = "john@example.com",
                Age = "14",
            };

            // Act
            var json = attendee.ToString();

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(json));
            Assert.IsTrue(json.Contains("12345"));
            Assert.IsTrue(json.Contains("John"));
            Assert.IsTrue(json.Contains("Smith"));
            Assert.IsTrue(json.Contains("john@example.com"));
            Assert.IsTrue(json.Contains("14"));
        }

        [TestMethod]
        public void Attendee_WithSpecialCharactersInName_HandlesCorrectly()
        {
            // Arrange
            var attendee = new Attendee
            {
                FirstName = "O'Brien",
                LastName = "De La Cruz",
            };

            // Act
            var fullName = attendee.FullName;

            // Assert
            Assert.AreEqual("O'Brien De La Cruz", fullName);
        }
    }
}
