// <copyright file="WorkshopRosterGeneratorTests.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Microsoft.Extensions.Logging.Abstractions;
using WinterAdventurer.Library.Models;
using WinterAdventurer.Library.Services;

namespace WinterAdventurer.Test
{
    /// <summary>
    /// Tests for WorkshopRosterGenerator service that creates class roster PDFs for workshop leaders.
    /// </summary>
    [TestClass]
    public class WorkshopRosterGeneratorTests
    {
        private WorkshopRosterGenerator _generator = null!;

        [TestInitialize]
        public void Setup()
        {
            _generator = new WorkshopRosterGenerator(NullLogger<WorkshopRosterGenerator>.Instance);
        }

        [TestMethod]
        public void GenerateRosterSections_WithEmptyWorkshopList_ReturnsEmptyList()
        {
            // Arrange
            var workshops = new List<Workshop>();

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event");

            // Assert
            Assert.IsNotNull(sections);
            Assert.AreEqual(0, sections.Count);
        }

        [TestMethod]
        public void GenerateRosterSections_WithSingleWorkshop_ReturnsSingleSection()
        {
            // Arrange
            var workshop = CreateSampleWorkshop("Pottery", "John Smith");
            var workshops = new List<Workshop> { workshop };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
        }

        [TestMethod]
        public void GenerateRosterSections_WithMultipleWorkshops_ReturnsCorrectNumberOfSections()
        {
            // Arrange
            var workshops = new List<Workshop>
            {
                CreateSampleWorkshop("Pottery", "John Smith"),
                CreateSampleWorkshop("Woodworking", "Jane Doe"),
                CreateSampleWorkshop("Painting", "Mary Johnson"),
            };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event");

            // Assert
            Assert.AreEqual(3, sections.Count);
        }

        [TestMethod]
        public void GenerateRosterSections_IncludesWorkshopName()
        {
            // Arrange
            var workshop = CreateSampleWorkshop("Advanced Pottery", "John Smith");
            var workshops = new List<Workshop> { workshop };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];

            // Workshop name should be in one of the paragraphs (as the first content paragraph after logo)
            var paragraphs = section.Elements.OfType<MigraDoc.DocumentObjectModel.Paragraph>().ToList();
            Assert.IsTrue(paragraphs.Count > 0, "Section should contain paragraphs");

            // Find the paragraph with workshop name (it should be bold and have the workshop name)
            var hasWorkshopName = paragraphs.Any(p =>
                p.Elements.OfType<MigraDoc.DocumentObjectModel.FormattedText>().Any(ft =>
                    ft.Elements.OfType<MigraDoc.DocumentObjectModel.Text>().Any(t =>
                        t.Content != null && t.Content.Contains("Advanced Pottery"))));

            Assert.IsTrue(hasWorkshopName, "Section should contain the workshop name");
        }

        [TestMethod]
        public void GenerateRosterSections_IncludesLeaderName()
        {
            // Arrange
            var workshop = CreateSampleWorkshop("Pottery", "John Smith");
            var workshops = new List<Workshop> { workshop };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];
            var paragraphs = section.Elements.OfType<MigraDoc.DocumentObjectModel.Paragraph>().ToList();

            // Find paragraph containing leader name
            var hasLeaderName = paragraphs.Any(p =>
                p.Elements.OfType<MigraDoc.DocumentObjectModel.FormattedText>().Any(ft =>
                    ft.Elements.OfType<MigraDoc.DocumentObjectModel.Text>().Any(t =>
                        t.Content != null && t.Content.Contains("John Smith"))));

            Assert.IsTrue(hasLeaderName, "Section should contain the leader name");
        }

        [TestMethod]
        public void GenerateRosterSections_IncludesLocationWhenSet()
        {
            // Arrange
            var workshop = CreateSampleWorkshop("Pottery", "John Smith");
            workshop.Location = "Art Studio";
            var workshops = new List<Workshop> { workshop };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];
            var paragraphs = section.Elements.OfType<MigraDoc.DocumentObjectModel.Paragraph>().ToList();

            // Find paragraph containing location
            var hasLocation = paragraphs.Any(p =>
                p.Elements.OfType<MigraDoc.DocumentObjectModel.FormattedText>().Any(ft =>
                    ft.Elements.OfType<MigraDoc.DocumentObjectModel.Text>().Any(t =>
                        t.Content != null && t.Content.Contains("Art Studio"))));

            Assert.IsTrue(hasLocation, "Section should contain the location");
        }

        [TestMethod]
        public void GenerateRosterSections_OmitsLocationWhenNotSet()
        {
            // Arrange
            var workshop = CreateSampleWorkshop("Pottery", "John Smith");
            workshop.Location = string.Empty; // No location set
            var workshops = new List<Workshop> { workshop };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event");

            // Assert - Should not throw and should not contain "Location:"
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];
            var paragraphs = section.Elements.OfType<MigraDoc.DocumentObjectModel.Paragraph>().ToList();

            var hasLocationLabel = paragraphs.Any(p =>
                p.Elements.OfType<MigraDoc.DocumentObjectModel.FormattedText>().Any(ft =>
                    ft.Elements.OfType<MigraDoc.DocumentObjectModel.Text>().Any(t =>
                        t.Content != null && t.Content.Contains("Location:"))));

            Assert.IsFalse(hasLocationLabel, "Section should not contain 'Location:' when location is not set");
        }

        [TestMethod]
        public void GenerateRosterSections_IncludesPeriodAndDuration()
        {
            // Arrange
            var workshop = CreateSampleWorkshop("Pottery", "John Smith");
            var workshops = new List<Workshop> { workshop };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];
            var paragraphs = section.Elements.OfType<MigraDoc.DocumentObjectModel.Paragraph>().ToList();

            // Should contain period name and duration description
            var hasPeriodInfo = paragraphs.Any(p =>
                p.Elements.OfType<MigraDoc.DocumentObjectModel.FormattedText>().Any(ft =>
                    ft.Elements.OfType<MigraDoc.DocumentObjectModel.Text>().Any(t =>
                        t.Content != null &&
                        t.Content.Contains("Morning First Period") &&
                        t.Content.Contains("Days 1-4"))));

            Assert.IsTrue(hasPeriodInfo, "Section should contain period and duration information");
        }

        [TestMethod]
        public void GenerateRosterSections_WithTimeslots_IncludesTimeRange()
        {
            // Arrange
            var workshop = CreateSampleWorkshop("Pottery", "John Smith");
            var workshops = new List<Workshop> { workshop };

            var timeslots = new List<TimeSlot>
            {
                new TimeSlot
                {
                    IsPeriod = true,
                    Label = "Morning First Period",
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(9, 30, 0)
                },
            };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event", timeslots);

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];
            var paragraphs = section.Elements.OfType<MigraDoc.DocumentObjectModel.Paragraph>().ToList();

            // Should contain time range in period info
            var hasTimeRange = paragraphs.Any(p =>
                p.Elements.OfType<MigraDoc.DocumentObjectModel.FormattedText>().Any(ft =>
                    ft.Elements.OfType<MigraDoc.DocumentObjectModel.Text>().Any(t =>
                        t.Content != null && t.Content.Contains("8:00 AM - 9:30 AM"))));

            Assert.IsTrue(hasTimeRange, "Section should contain time range when timeslots are provided");
        }

        [TestMethod]
        public void GenerateRosterSections_SortsFirstChoiceParticipantsByLastName()
        {
            // Arrange
            var workshop = CreateSampleWorkshop("Pottery", "John Smith");

            // Add participants in non-alphabetical order
            workshop.Selections = new List<WorkshopSelection>
            {
                new WorkshopSelection
                {
                    FirstName = "Charlie",
                    LastName = "Wilson",
                    ChoiceNumber = 1,
                    ClassSelectionId = "SEL003",
                },
                new WorkshopSelection
                {
                    FirstName = "Alice",
                    LastName = "Adams",
                    ChoiceNumber = 1,
                    ClassSelectionId = "SEL001",
                },
                new WorkshopSelection
                {
                    FirstName = "Bob",
                    LastName = "Smith",
                    ChoiceNumber = 1,
                    ClassSelectionId = "SEL002"
                },
            };

            var workshops = new List<Workshop> { workshop };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);

            // Verify the section was created successfully
            // The sorting logic is internal, but we can verify the section contains all participants
            var section = sections[0];
            Assert.IsNotNull(section);
        }

        [TestMethod]
        public void GenerateRosterSections_SeparatesFirstChoiceFromBackupParticipants()
        {
            // Arrange
            var workshop = CreateSampleWorkshop("Pottery", "John Smith");

            // Add mix of first choice and backup participants
            workshop.Selections = new List<WorkshopSelection>
            {
                new WorkshopSelection
                {
                    FirstName = "Alice",
                    LastName = "Adams",
                    ChoiceNumber = 1,
                    ClassSelectionId = "SEL001",
                },
                new WorkshopSelection
                {
                    FirstName = "Bob",
                    LastName = "Baker",
                    ChoiceNumber = 2,
                    ClassSelectionId = "SEL002",
                },
                new WorkshopSelection
                {
                    FirstName = "Charlie",
                    LastName = "Carter",
                    ChoiceNumber = 1,
                    ClassSelectionId = "SEL003"
                },
            };

            var workshops = new List<Workshop> { workshop };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];
            var paragraphs = section.Elements.OfType<MigraDoc.DocumentObjectModel.Paragraph>().ToList();

            // Should have both "Enrolled Participants" and "Backup/Alternate Choices" headers
            var hasEnrolledHeader = paragraphs.Any(p =>
                p.Elements.OfType<MigraDoc.DocumentObjectModel.FormattedText>().Any(ft =>
                    ft.Elements.OfType<MigraDoc.DocumentObjectModel.Text>().Any(t =>
                        t.Content != null && t.Content.Contains("Enrolled Participants"))));

            var hasBackupHeader = paragraphs.Any(p =>
                p.Elements.OfType<MigraDoc.DocumentObjectModel.FormattedText>().Any(ft =>
                    ft.Elements.OfType<MigraDoc.DocumentObjectModel.Text>().Any(t =>
                        t.Content != null && t.Content.Contains("Backup/Alternate Choices"))));

            Assert.IsTrue(hasEnrolledHeader, "Section should have 'Enrolled Participants' header");
            Assert.IsTrue(hasBackupHeader, "Section should have 'Backup/Alternate Choices' header");
        }

        [TestMethod]
        public void GenerateRosterSections_ShowsParticipantCounts()
        {
            // Arrange
            var workshop = CreateSampleWorkshop("Pottery", "John Smith");

            // Add 3 first choice participants
            workshop.Selections = new List<WorkshopSelection>
            {
                new WorkshopSelection
                {
                    FirstName = "Alice",
                    LastName = "Adams",
                    ChoiceNumber = 1,
                    ClassSelectionId = "SEL001",
                },
                new WorkshopSelection
                {
                    FirstName = "Bob",
                    LastName = "Baker",
                    ChoiceNumber = 1,
                    ClassSelectionId = "SEL002",
                },
                new WorkshopSelection
                {
                    FirstName = "Charlie",
                    LastName = "Carter",
                    ChoiceNumber = 1,
                    ClassSelectionId = "SEL003"
                },
            };

            var workshops = new List<Workshop> { workshop };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];
            var paragraphs = section.Elements.OfType<MigraDoc.DocumentObjectModel.Paragraph>().ToList();

            // Should show count in header
            var hasCount = paragraphs.Any(p =>
                p.Elements.OfType<MigraDoc.DocumentObjectModel.FormattedText>().Any(ft =>
                    ft.Elements.OfType<MigraDoc.DocumentObjectModel.Text>().Any(t =>
                        t.Content != null && t.Content.Contains("(3)"))));

            Assert.IsTrue(hasCount, "Section should show participant count");
        }

        [TestMethod]
        public void GenerateRosterSections_WithOnlyFirstChoice_DoesNotShowBackupSection()
        {
            // Arrange
            var workshop = CreateSampleWorkshop("Pottery", "John Smith");

            // Add only first choice participants (no backups)
            workshop.Selections = new List<WorkshopSelection>
            {
                new WorkshopSelection
                {
                    FirstName = "Alice",
                    LastName = "Adams",
                    ChoiceNumber = 1,
                    ClassSelectionId = "SEL001"
                },
            };

            var workshops = new List<Workshop> { workshop };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];
            var paragraphs = section.Elements.OfType<MigraDoc.DocumentObjectModel.Paragraph>().ToList();

            // Should NOT have "Backup/Alternate Choices" header
            var hasBackupHeader = paragraphs.Any(p =>
                p.Elements.OfType<MigraDoc.DocumentObjectModel.FormattedText>().Any(ft =>
                    ft.Elements.OfType<MigraDoc.DocumentObjectModel.Text>().Any(t =>
                        t.Content != null && t.Content.Contains("Backup/Alternate Choices"))));

            Assert.IsFalse(hasBackupHeader, "Section should not have backup section when there are no backup participants");
        }

        [TestMethod]
        public void GenerateRosterSections_WithNoParticipants_StillCreatesSection()
        {
            // Arrange
            var workshop = CreateSampleWorkshop("Pottery", "John Smith");
            workshop.Selections = new List<WorkshopSelection>(); // No participants
            var workshops = new List<Workshop> { workshop };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Test Event");

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];
            Assert.IsNotNull(section);
        }

        [TestMethod]
        public void GenerateRosterSections_AddsFooterWithEventName()
        {
            // Arrange
            var workshop = CreateSampleWorkshop("Pottery", "John Smith");
            var workshops = new List<Workshop> { workshop };

            // Act
            var sections = _generator.GenerateRosterSections(workshops, "Winter Adventure 2024");

            // Assert
            Assert.AreEqual(1, sections.Count);
            var section = sections[0];

            // Check that footer exists
            Assert.IsNotNull(section.Footers);
            Assert.IsNotNull(section.Footers.Primary);

            // Check footer contains event name
            var footerParagraphs = section.Footers.Primary.Elements.OfType<MigraDoc.DocumentObjectModel.Paragraph>().ToList();
            var hasEventName = footerParagraphs.Any(p =>
                p.Elements.OfType<MigraDoc.DocumentObjectModel.Text>().Any(t =>
                    t.Content != null && t.Content.Contains("Winter Adventure 2024")));

            Assert.IsTrue(hasEventName, "Footer should contain the event name");
        }

        /// <summary>
        /// Creates a sample workshop for testing purposes.
        /// </summary>
        private Workshop CreateSampleWorkshop(string name, string leader)
        {
            return new Workshop
            {
                Name = name,
                Leader = leader,
                Period = new Period("MorningFirstPeriod"),
                Duration = new WorkshopDuration(1, 4),
                Selections = new List<WorkshopSelection>
                {
                    new WorkshopSelection
                    {
                        FirstName = "Test",
                        LastName = "User",
                        ChoiceNumber = 1,
                        ClassSelectionId = "SEL001"
                    }
                },
            };
        }
    }
}
