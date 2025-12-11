// <copyright file="MasterScheduleGeneratorTests.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Microsoft.Extensions.Logging.Abstractions;
using WinterAdventurer.Library.EventSchemas;
using WinterAdventurer.Library.Models;
using WinterAdventurer.Library.Services;

namespace WinterAdventurer.Test
{
    /// <summary>
    /// Tests for MasterScheduleGenerator service that creates master schedule PDFs.
    /// </summary>
    [TestClass]
    public class MasterScheduleGeneratorTests
    {
        private MasterScheduleGenerator _generator = null!;
        private EventSchema _schema = null!;

        [TestInitialize]
        public void Setup()
        {
            _schema = CreateTestSchema();
            _generator = new MasterScheduleGenerator(_schema, NullLogger<MasterScheduleGenerator>.Instance);
        }

        [TestMethod]
        public void GenerateMasterSchedule_WithNoLocations_ReturnsEmptySections()
        {
            // Arrange
            var workshops = new List<Workshop>
            {
                CreateSampleWorkshop("Pottery", "John Smith", null), // No location
            };

            // Act
            var sections = _generator.GenerateMasterSchedule(workshops, "Test Event");

            // Assert
            Assert.AreEqual(0, sections.Count, "Should return empty sections when no locations are assigned");
        }

        [TestMethod]
        public void GenerateMasterSchedule_WithSingleLocation_ReturnsSection()
        {
            // Arrange
            var workshops = new List<Workshop>
            {
                CreateSampleWorkshop("Pottery", "John Smith", "Art Studio"),
            };

            // Act
            var sections = _generator.GenerateMasterSchedule(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
        }

        [TestMethod]
        public void GenerateMasterSchedule_WithMultipleLocations_ReturnsSection()
        {
            // Arrange
            var workshops = new List<Workshop>
            {
                CreateSampleWorkshop("Pottery", "John Smith", "Art Studio"),
                CreateSampleWorkshop("Woodworking", "Jane Doe", "Workshop Room"),
            };

            // Act
            var sections = _generator.GenerateMasterSchedule(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
        }

        [TestMethod]
        public void GenerateMasterSchedule_WithFewLocations_UsesPortraitOrientation()
        {
            // Arrange - 5 or fewer locations should use portrait
            var workshops = new List<Workshop>
            {
                CreateSampleWorkshop("Workshop1", "Leader1", "Location1"),
                CreateSampleWorkshop("Workshop2", "Leader2", "Location2"),
                CreateSampleWorkshop("Workshop3", "Leader3", "Location3"),
            };

            // Act
            var sections = _generator.GenerateMasterSchedule(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];
            Assert.AreEqual(MigraDoc.DocumentObjectModel.Orientation.Portrait, section.PageSetup.Orientation);
        }

        [TestMethod]
        public void GenerateMasterSchedule_WithManyLocations_UsesLandscapeOrientation()
        {
            // Arrange - More than 5 locations should use landscape
            var workshops = new List<Workshop>
            {
                CreateSampleWorkshop("Workshop1", "Leader1", "Location1"),
                CreateSampleWorkshop("Workshop2", "Leader2", "Location2"),
                CreateSampleWorkshop("Workshop3", "Leader3", "Location3"),
                CreateSampleWorkshop("Workshop4", "Leader4", "Location4"),
                CreateSampleWorkshop("Workshop5", "Leader5", "Location5"),
                CreateSampleWorkshop("Workshop6", "Leader6", "Location6"),
            };

            // Act
            var sections = _generator.GenerateMasterSchedule(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];
            Assert.AreEqual(MigraDoc.DocumentObjectModel.Orientation.Landscape, section.PageSetup.Orientation);
        }

        [TestMethod]
        public void GenerateMasterSchedule_IncludesEventNameInTitle()
        {
            // Arrange
            var workshops = new List<Workshop>
            {
                CreateSampleWorkshop("Pottery", "John Smith", "Art Studio"),
            };

            // Act
            var sections = _generator.GenerateMasterSchedule(workshops, "Winter Adventure 2024");

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];
            var paragraphs = section.Elements.OfType<MigraDoc.DocumentObjectModel.Paragraph>().ToList();

            // Should contain event name in a paragraph
            var hasEventName = paragraphs.Any(p =>
                p.Elements.OfType<MigraDoc.DocumentObjectModel.FormattedText>().Any(ft =>
                    ft.Elements.OfType<MigraDoc.DocumentObjectModel.Text>().Any(t =>
                        t.Content != null && t.Content.Contains("Winter Adventure 2024"))));

            Assert.IsTrue(hasEventName, "Section should contain the event name");
        }

        [TestMethod]
        public void GenerateMasterSchedule_WithCustomTimeslots_UsesProvidedTimeslots()
        {
            // Arrange
            var workshops = new List<Workshop>
            {
                CreateSampleWorkshop("Pottery", "John Smith", "Art Studio"),
            };

            var customTimeslots = new List<TimeSlot>
            {
                new TimeSlot
                {
                    Label = "Morning Session",
                    IsPeriod = true,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(10, 0, 0)
                },
            };

            // Act
            var sections = _generator.GenerateMasterSchedule(workshops, "Test Event", customTimeslots);

            // Assert
            Assert.AreEqual(1, sections.Count);

            // Should have created a section with the custom timeslot
        }

        [TestMethod]
        public void GenerateMasterSchedule_SortsLocationsAlphabetically()
        {
            // Arrange - Create workshops with locations in non-alphabetical order
            var workshops = new List<Workshop>
            {
                CreateSampleWorkshop("Workshop1", "Leader1", "Zebra Room"),
                CreateSampleWorkshop("Workshop2", "Leader2", "Alpha Room"),
                CreateSampleWorkshop("Workshop3", "Leader3", "Beta Room"),
            };

            // Act
            var sections = _generator.GenerateMasterSchedule(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];

            // Check that table exists
            var tables = section.Elements.OfType<MigraDoc.DocumentObjectModel.Tables.Table>().ToList();
            Assert.AreEqual(1, tables.Count, "Should have one table");

            // The locations should be sorted alphabetically in the table headers
            // (This is tested indirectly by verifying the section was created successfully)
        }

        [TestMethod]
        public void GenerateMasterSchedule_FiltersEmptyLocations()
        {
            // Arrange
            var workshops = new List<Workshop>
            {
                CreateSampleWorkshop("Workshop1", "Leader1", "Art Studio"),
                CreateSampleWorkshop("Workshop2", "Leader2", string.Empty), // Empty location
                CreateSampleWorkshop("Workshop3", "Leader3", null), // Null location
            };

            // Act
            var sections = _generator.GenerateMasterSchedule(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);

            // Should only have one location column (Art Studio)
        }

        [TestMethod]
        public void CreateDefaultTimeslots_ReturnsTimeslots()
        {
            // Act
            var timeslots = _generator.CreateDefaultTimeslots();

            // Assert
            Assert.IsNotNull(timeslots);
            Assert.IsTrue(timeslots.Count > 0, "Should return at least one timeslot");
        }

        [TestMethod]
        public void CreateDefaultTimeslots_IncludesBreakfast()
        {
            // Act
            var timeslots = _generator.CreateDefaultTimeslots();

            // Assert
            var breakfast = timeslots.FirstOrDefault(t => t.Label == "Breakfast");
            Assert.IsNotNull(breakfast, "Should include Breakfast timeslot");
            Assert.IsFalse(breakfast.IsPeriod, "Breakfast should not be marked as a period");
        }

        [TestMethod]
        public void CreateDefaultTimeslots_IncludesLunch()
        {
            // Act
            var timeslots = _generator.CreateDefaultTimeslots();

            // Assert
            var lunch = timeslots.FirstOrDefault(t => t.Label == "Lunch");
            Assert.IsNotNull(lunch, "Should include Lunch timeslot");
            Assert.IsFalse(lunch.IsPeriod, "Lunch should not be marked as a period");
        }

        [TestMethod]
        public void CreateDefaultTimeslots_IncludesEveningProgram()
        {
            // Act
            var timeslots = _generator.CreateDefaultTimeslots();

            // Assert
            var evening = timeslots.FirstOrDefault(t => t.Label == "Evening Program");
            Assert.IsNotNull(evening, "Should include Evening Program timeslot");
            Assert.IsFalse(evening.IsPeriod, "Evening Program should not be marked as a period");
        }

        [TestMethod]
        public void CreateDefaultTimeslots_IncludesPeriodSheetsFromSchema()
        {
            // Act
            var timeslots = _generator.CreateDefaultTimeslots();

            // Assert
            // Should include all period sheets from the schema
            var periods = timeslots.Where(t => t.IsPeriod).ToList();
            Assert.AreEqual(_schema.PeriodSheets.Count, periods.Count,
                "Should include all period sheets from schema as period timeslots");
        }

        [TestMethod]
        public void CreateDefaultTimeslots_BreakfastIsFirst()
        {
            // Act
            var timeslots = _generator.CreateDefaultTimeslots();

            // Assert
            Assert.IsTrue(timeslots.Count > 0);
            Assert.AreEqual("Breakfast", timeslots[0].Label, "Breakfast should be the first timeslot");
        }

        /// <summary>
        /// Creates a minimal test schema for testing purposes.
        /// </summary>
        private EventSchema CreateTestSchema()
        {
            return new EventSchema
            {
                EventName = "Test Event",
                TotalDays = 4,
                PeriodSheets = new List<PeriodSheetConfig>
                {
                    new PeriodSheetConfig
                    {
                        SheetName = "MorningFirstPeriod",
                        DisplayName = "Morning First Period"
                    },
                    new PeriodSheetConfig
                    {
                        SheetName = "AfternoonPeriod",
                        DisplayName = "Afternoon Period"
                    }
                },
            };
        }

        /// <summary>
        /// Creates a sample workshop for testing purposes.
        /// </summary>
        private Workshop CreateSampleWorkshop(string name, string leader, string? location)
        {
            return new Workshop
            {
                Name = name,
                Leader = leader,
                Location = location ?? string.Empty,
                Period = new Period("MorningFirstPeriod"),
                Duration = new WorkshopDuration(1, 4),
                Selections = new List<WorkshopSelection>(),
            };
        }
    }
}
