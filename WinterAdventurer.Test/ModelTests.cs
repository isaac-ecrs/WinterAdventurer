using WinterAdventurer.Library.Models;

namespace WinterAdventurer.Test
{
    [TestClass]
    public class ModelTests
    {
        #region WorkshopDuration Tests

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

        #endregion

        #region Period Tests

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

        #endregion

        #region Workshop Tests

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
                Duration = duration
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
                Duration = duration
            };

            var workshop2 = new Workshop
            {
                Name = "Pottery",
                Leader = "Jane Doe",
                Period = period,
                Duration = duration
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
                Duration = duration1
            };

            var workshop2 = new Workshop
            {
                Name = "Pottery",
                Leader = "John Smith",
                Period = period,
                Duration = duration2
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
                Duration = duration
            };

            var workshop2 = new Workshop
            {
                Name = "Pottery",
                Leader = "John Smith",
                Period = period2,
                Duration = duration
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
                Duration = new WorkshopDuration(1, 4)
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

        #endregion

        #region Integration Tests

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
                MinAge = 12
            };

            var selection = new WorkshopSelection
            {
                ChoiceNumber = 1,
                ClassSelectionId = "12345"
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
                new { SheetName = "LunchBreak", Expected = "Lunch Break" }
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
                new { Start = 2, End = 2, Days = 1, Desc = "Day 2" }
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

        #endregion
    }
}
